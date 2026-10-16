namespace WinTools;

// Zstandard decompressor (RFC 8878) for the objects inside .xcresult bundles: Windows has no zstd
// codec. Frames, raw/RLE/compressed blocks, Huffman literals and FSE sequences; no dictionaries.
static class XcResultZstd
{
    public static byte[] Decompress(byte[] input)
    {
        var output = new Output(Math.Max(input.Length * 4, 1024));
        var pos = 0;
        while (pos + 4 <= input.Length)
        {
            var magic = BitConverter.ToUInt32(input, pos);
            if ((magic & 0xFFFFFFF0) == 0x184D2A50)
            {
                pos += 8 + (int)BitConverter.ToUInt32(input, pos + 4);   // skippable frame
                continue;
            }
            if (magic != 0xFD2FB528) throw new InvalidDataException("not zstd data");
            pos = new Frame(input, pos + 4, output).Decode();
        }
        return output.ToArray();
    }

    sealed class Output(int capacity)
    {
        byte[] data = new byte[capacity];
        public int Length;

        void Grow(int extra)
        {
            if (Length + extra <= data.Length) return;
            Array.Resize(ref data, Math.Max(data.Length * 2, Length + extra));
        }

        public void Write(byte[] source, int offset, int count)
        {
            Grow(count);
            Buffer.BlockCopy(source, offset, data, Length, count);
            Length += count;
        }

        public void Fill(byte value, int count)
        {
            Grow(count);
            Array.Fill(data, value, Length, count);
            Length += count;
        }

        public void CopyMatch(int offset, int count)
        {
            if (offset <= 0 || offset > Length) throw new InvalidDataException("zstd: bad match offset");
            Grow(count);
            var from = Length - offset;
            for (var i = 0; i < count; i++) data[Length + i] = data[from + i];   // may overlap
            Length += count;
        }

        public byte[] ToArray() => data.AsSpan(0, Length).ToArray();
    }

    // Bits read from the end of a stream towards its start (zstd's backward bitstreams).
    sealed class BackwardBits
    {
        readonly byte[] data;
        readonly int start, end;
        public long Position;   // unread bits remaining; below zero reads zeros

        public BackwardBits(byte[] data, int start, int end)
        {
            this.data = data; this.start = start; this.end = end;
            if (end <= start) throw new InvalidDataException("zstd: empty bitstream");
            var last = data[end - 1];
            if (last == 0) throw new InvalidDataException("zstd: bad bitstream padding");
            Position = (long)(end - start - 1) * 8 + (31 - System.Numerics.BitOperations.LeadingZeroCount((uint)last));
        }

        public int Peek(int n)
        {
            if (n == 0) return 0;
            var low = Position - n;
            if (low >= 0) return (int)(Fetch(low) & ((1UL << n) - 1));
            if (Position <= 0) return 0;
            return (int)((Fetch(0) & ((1UL << (int)Position) - 1)) << (int)-low);
        }

        ulong Fetch(long bit)
        {
            var index = start + (int)(bit >> 3);
            ulong v = 0;
            for (var i = 0; i < 8 && index + i < end; i++) v |= (ulong)data[index + i] << (8 * i);
            return v >> (int)(bit & 7);
        }

        public int Read(int n) { var v = Peek(n); Position -= n; return v; }
    }

    sealed class FseTable
    {
        public byte[] Symbol;
        public byte[] Bits;
        public int[] Base;
        public int Log;

        public static FseTable Rle(byte symbol) => new() { Symbol = [symbol], Bits = [0], Base = [0], Log = 0 };

        public static FseTable Build(short[] probabilities, int count, int log)
        {
            var size = 1 << log;
            var t = new FseTable { Symbol = new byte[size], Bits = new byte[size], Base = new int[size], Log = log };
            var next = new int[count];
            var high = size - 1;
            for (var s = 0; s < count; s++)
            {
                if (probabilities[s] == -1) { t.Symbol[high--] = (byte)s; next[s] = 1; }
                else next[s] = probabilities[s];
            }
            int step = (size >> 1) + (size >> 3) + 3, mask = size - 1, pos = 0;
            for (var s = 0; s < count; s++)
                for (var i = 0; i < probabilities[s]; i++)
                {
                    t.Symbol[pos] = (byte)s;
                    do pos = (pos + step) & mask; while (pos > high);
                }
            for (var u = 0; u < size; u++)
            {
                var state = next[t.Symbol[u]]++;
                var bits = log - (31 - System.Numerics.BitOperations.LeadingZeroCount((uint)state));
                t.Bits[u] = (byte)bits;
                t.Base[u] = (state << bits) - size;
            }
            return t;
        }

        // FSE table description (RFC 8878 4.1.1); returns bytes consumed.
        public static FseTable Read(byte[] src, int offset, int end, int maxLog, int maxSymbol, out int consumed)
        {
            long bit = 0;
            int ReadBit(int n)
            {
                var v = 0;
                for (var i = 0; i < n; i++)
                {
                    var b = offset + (int)((bit + i) >> 3);
                    if (b < end && (src[b] >> (int)((bit + i) & 7) & 1) != 0) v |= 1 << i;
                }
                return v;
            }
            int PeekBits(int n) => ReadBit(n);
            var log = PeekBits(4) + 5; bit += 4;
            if (log > maxLog) throw new InvalidDataException("zstd: FSE accuracy too large");
            var probabilities = new short[maxSymbol + 1];
            int remaining = (1 << log) + 1, threshold = 1 << log, nbBits = log + 1, symbol = 0;
            while (remaining > 1 && symbol <= maxSymbol)
            {
                var max = (2 * threshold - 1) - remaining;
                int value;
                var low = PeekBits(nbBits - 1);
                if (low < max) { value = low; bit += nbBits - 1; }
                else
                {
                    value = PeekBits(nbBits);
                    if (value >= threshold) value -= max;
                    bit += nbBits;
                }
                value--;
                remaining -= Math.Abs(value);
                probabilities[symbol++] = (short)value;
                if (value == 0)
                {
                    while (true)
                    {
                        var repeat = PeekBits(2); bit += 2;
                        symbol += repeat;
                        if (repeat != 3) break;
                    }
                }
                while (remaining < threshold) { nbBits--; threshold >>= 1; }
            }
            if (remaining != 1 || symbol > maxSymbol + 1) throw new InvalidDataException("zstd: bad FSE table");
            consumed = (int)((bit + 7) >> 3);
            return Build(probabilities, symbol, log);
        }
    }

    sealed class Huffman
    {
        public byte[] Symbol;
        public byte[] Bits;
        public int MaxBits;

        public static Huffman Read(byte[] src, int offset, out int consumed)
        {
            int header = src[offset];
            var weights = new byte[256];
            int count;
            if (header < 128)
            {
                // FSE-compressed weights, decoded by two interleaved states.
                var table = FseTable.Read(src, offset + 1, offset + 1 + header, 6, 255, out var used);
                var bits = new BackwardBits(src, offset + 1 + used, offset + 1 + header);
                int s1 = bits.Read(table.Log), s2 = bits.Read(table.Log);
                count = 0;
                while (true)
                {
                    weights[count++] = table.Symbol[s1];
                    s1 = table.Base[s1] + bits.Read(table.Bits[s1]);
                    if (bits.Position < 0) { weights[count++] = table.Symbol[s2]; break; }
                    weights[count++] = table.Symbol[s2];
                    s2 = table.Base[s2] + bits.Read(table.Bits[s2]);
                    if (bits.Position < 0) { weights[count++] = table.Symbol[s1]; break; }
                    if (count >= 254) throw new InvalidDataException("zstd: too many Huffman weights");
                }
                consumed = 1 + header;
            }
            else
            {
                count = header - 127;
                for (var i = 0; i < count; i++)
                {
                    var b = src[offset + 1 + i / 2];
                    weights[i] = (byte)(i % 2 == 0 ? b >> 4 : b & 15);
                }
                consumed = 1 + (count + 1) / 2;
            }
            long total = 0;
            for (var i = 0; i < count; i++) if (weights[i] > 0) total += 1L << (weights[i] - 1);
            if (total == 0) throw new InvalidDataException("zstd: bad Huffman weights");
            var maxBits = 64 - System.Numerics.BitOperations.LeadingZeroCount((ulong)total);   // highbit + 1
            var rest = (1L << maxBits) - total;
            if ((rest & (rest - 1)) != 0) throw new InvalidDataException("zstd: bad Huffman weights");
            weights[count++] = (byte)(System.Numerics.BitOperations.Log2((ulong)rest) + 1);

            var h = new Huffman { MaxBits = maxBits, Symbol = new byte[1 << maxBits], Bits = new byte[1 << maxBits] };
            var rankStart = new int[maxBits + 2];
            var rankCount = new int[maxBits + 2];
            for (var i = 0; i < count; i++) rankCount[weights[i]]++;
            var next = 0;
            for (var w = 1; w <= maxBits; w++) { rankStart[w] = next; next += rankCount[w] << (w - 1); }
            for (var i = 0; i < count; i++)
            {
                var w = weights[i];
                if (w == 0) continue;
                var length = (1 << w) >> 1;
                for (var u = rankStart[w]; u < rankStart[w] + length; u++)
                {
                    h.Symbol[u] = (byte)i;
                    h.Bits[u] = (byte)(maxBits + 1 - w);
                }
                rankStart[w] += length;
            }
            return h;
        }

        public void DecodeStream(byte[] src, int start, int end, byte[] dest, int destOffset, int count)
        {
            var bits = new BackwardBits(src, start, end);
            for (var i = 0; i < count; i++)
            {
                var index = bits.Peek(MaxBits);
                dest[destOffset + i] = Symbol[index];
                bits.Position -= Bits[index];
            }
        }
    }

    static readonly short[] LlDefault = [4, 3, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 2, 1, 1, 1, 1, 1, -1, -1, -1, -1];
    static readonly short[] MlDefault = [1, 4, 3, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, -1, -1, -1, -1, -1, -1, -1];
    static readonly short[] OfDefault = [1, 1, 1, 1, 1, 1, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, -1, -1, -1, -1, -1];
    static readonly int[] LlBase = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 18, 20, 22, 24, 28, 32, 40, 48, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536];
    static readonly byte[] LlBits = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 3, 3, 4, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
    static readonly int[] MlBase = [3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 37, 39, 41, 43, 47, 51, 59, 67, 83, 99, 131, 259, 515, 1027, 2051, 4099, 8195, 16387, 32771, 65539];
    static readonly byte[] MlBits = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 3, 3, 4, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];

    sealed class Frame(byte[] src, int pos, Output output)
    {
        Huffman huffman;
        FseTable ll, of, ml;
        readonly int[] rep = [1, 4, 8];

        public int Decode()
        {
            var descriptor = src[pos++];
            var fcsFlag = descriptor >> 6;
            var singleSegment = (descriptor & 0x20) != 0;
            var checksum = (descriptor & 0x04) != 0;
            var dictFlag = descriptor & 3;
            if (!singleSegment) pos++;   // window descriptor
            if (dictFlag != 0)
            {
                var dictBytes = dictFlag == 3 ? 4 : dictFlag;
                for (var i = 0; i < dictBytes; i++) if (src[pos + i] != 0) throw new InvalidDataException("zstd: dictionaries are not supported");
                pos += dictBytes;
            }
            pos += fcsFlag switch { 0 => singleSegment ? 1 : 0, 1 => 2, 2 => 4, _ => 8 };
            var frameStart = output.Length;
            while (true)
            {
                var header = src[pos] | src[pos + 1] << 8 | src[pos + 2] << 16;
                pos += 3;
                var last = (header & 1) != 0;
                var type = (header >> 1) & 3;
                var size = header >> 3;
                switch (type)
                {
                    case 0: output.Write(src, pos, size); pos += size; break;
                    case 1: output.Fill(src[pos], size); pos += 1; break;
                    case 2: Block(pos, pos + size, frameStart); pos += size; break;
                    default: throw new InvalidDataException("zstd: reserved block type");
                }
                if (last) break;
            }
            if (checksum) pos += 4;
            return pos;
        }

        void Block(int p, int end, int frameStart)
        {
            // Literals section.
            var b0 = src[p];
            var litType = b0 & 3;
            var sizeFormat = (b0 >> 2) & 3;
            byte[] literals;
            if (litType is 0 or 1)
            {
                int regenerated;
                switch (sizeFormat)
                {
                    case 0 or 2: regenerated = b0 >> 3; p += 1; break;
                    case 1: regenerated = (b0 >> 4) + (src[p + 1] << 4); p += 2; break;
                    default: regenerated = (b0 >> 4) + (src[p + 1] << 4) + (src[p + 2] << 12); p += 3; break;
                }
                literals = new byte[regenerated];
                if (litType == 0) { Buffer.BlockCopy(src, p, literals, 0, regenerated); p += regenerated; }
                else { Array.Fill(literals, src[p]); p += 1; }
            }
            else
            {
                int regenerated, compressed, streams = sizeFormat == 0 ? 1 : 4;
                long h;
                switch (sizeFormat)
                {
                    case 0 or 1:
                        h = src[p] | src[p + 1] << 8 | src[p + 2] << 16;
                        regenerated = (int)((h >> 4) & 0x3FF); compressed = (int)((h >> 14) & 0x3FF); p += 3; break;
                    case 2:
                        h = (long)(src[p] | src[p + 1] << 8 | src[p + 2] << 16) | (long)src[p + 3] << 24;
                        regenerated = (int)((h >> 4) & 0x3FFF); compressed = (int)((h >> 18) & 0x3FFF); p += 4; break;
                    default:
                        h = (long)(src[p] | src[p + 1] << 8 | src[p + 2] << 16) | (long)src[p + 3] << 24 | (long)src[p + 4] << 32;
                        regenerated = (int)((h >> 4) & 0x3FFFF); compressed = (int)((h >> 22) & 0x3FFFF); p += 5; break;
                }
                var litEnd = p + compressed;
                if (litType == 2) { huffman = Huffman.Read(src, p, out var used); p += used; }
                else if (huffman == null) throw new InvalidDataException("zstd: treeless literals without a table");
                literals = new byte[regenerated];
                if (streams == 1) huffman.DecodeStream(src, p, litEnd, literals, 0, regenerated);
                else
                {
                    int s1 = src[p] | src[p + 1] << 8, s2 = src[p + 2] | src[p + 3] << 8, s3 = src[p + 4] | src[p + 5] << 8;
                    var start = p + 6;
                    var segment = (regenerated + 3) / 4;
                    int[] starts = [start, start + s1, start + s1 + s2, start + s1 + s2 + s3, litEnd];
                    for (var i = 0; i < 4; i++)
                    {
                        var count = i < 3 ? segment : regenerated - 3 * segment;
                        huffman.DecodeStream(src, starts[i], starts[i + 1], literals, i * segment, count);
                    }
                }
                p = litEnd;
            }

            // Sequences section.
            var litPos = 0;
            int sequences = 0;
            if (p < end)
            {
                int n0 = src[p];
                if (n0 == 0) p++;
                else if (n0 < 128) { sequences = n0; p++; }
                else if (n0 < 255) { sequences = ((n0 - 128) << 8) + src[p + 1]; p += 2; }
                else { sequences = src[p + 1] + (src[p + 2] << 8) + 0x7F00; p += 3; }
            }
            if (sequences > 0)
            {
                var modes = src[p++];
                ll = Table(modes >> 6, ref p, end, ll, LlDefault, 6, 9, 35);
                of = Table((modes >> 4) & 3, ref p, end, of, OfDefault, 5, 8, 31);
                ml = Table((modes >> 2) & 3, ref p, end, ml, MlDefault, 6, 9, 52);
                var bits = new BackwardBits(src, p, end);
                int sLl = bits.Read(ll.Log), sOf = bits.Read(of.Log), sMl = bits.Read(ml.Log);
                for (var i = 0; i < sequences; i++)
                {
                    int ofCode = of.Symbol[sOf], mlCode = ml.Symbol[sMl], llCode = ll.Symbol[sLl];
                    if (ofCode > 31) throw new InvalidDataException("zstd: bad offset code");
                    var offsetValue = (1 << ofCode) + bits.Read(ofCode);
                    var matchLength = MlBase[mlCode] + bits.Read(MlBits[mlCode]);
                    var literalLength = LlBase[llCode] + bits.Read(LlBits[llCode]);
                    int offset;
                    if (offsetValue > 3)
                    {
                        offset = offsetValue - 3;
                        rep[2] = rep[1]; rep[1] = rep[0]; rep[0] = offset;
                    }
                    else
                    {
                        var index = offsetValue - 1 + (literalLength == 0 ? 1 : 0);
                        if (index == 0) offset = rep[0];
                        else
                        {
                            offset = index == 3 ? rep[0] - 1 : rep[index];
                            if (offset == 0) offset = 1;
                            if (index != 1) rep[2] = rep[1];
                            rep[1] = rep[0];
                            rep[0] = offset;
                        }
                    }
                    if (i < sequences - 1)
                    {
                        sLl = ll.Base[sLl] + bits.Read(ll.Bits[sLl]);
                        sMl = ml.Base[sMl] + bits.Read(ml.Bits[sMl]);
                        sOf = of.Base[sOf] + bits.Read(of.Bits[sOf]);
                    }
                    if (litPos + literalLength > literals.Length) throw new InvalidDataException("zstd: literal overrun");
                    output.Write(literals, litPos, literalLength);
                    litPos += literalLength;
                    output.CopyMatch(offset, matchLength);
                }
            }
            output.Write(literals, litPos, literals.Length - litPos);
            _ = frameStart;
        }

        FseTable Table(int mode, ref int p, int end, FseTable previous, short[] defaults, int defaultLog, int maxLog, int maxSymbol)
        {
            switch (mode)
            {
                case 0: return FseTable.Build(defaults, defaults.Length, defaultLog);
                case 1: return FseTable.Rle(src[p++]);
                case 2:
                    var table = FseTable.Read(src, p, end, maxLog, maxSymbol, out var used);
                    p += used;
                    return table;
                default: return previous ?? throw new InvalidDataException("zstd: repeat mode without a table");
            }
        }
    }
}
