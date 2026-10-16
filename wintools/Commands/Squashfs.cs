using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace WinTools;

// squashfs 4.0 reader for squashfuse: walks the directory table from the root inode and writes
// every file, folder and symlink out. Metadata and data may be zlib-compressed (the mksquashfs
// default) or stored; the other compressors (xz, lzo, lz4, zstd) have no decoder in Windows.
static class Squashfs
{
    const uint Magic = 0x73717368;
    const uint NoFragment = 0xFFFFFFFF;

    sealed class Image(FileStream file, long offset)
    {
        public readonly FileStream File = file;
        public readonly long Offset = offset;
        public int Compression, BlockSize;
        public long InodeTable, DirectoryTable, FragmentTable;
        public uint Fragments;
        readonly Dictionary<long, (byte[] Data, long Next)> metadata = [];

        public byte[] ReadAt(long position, int count)
        {
            var buffer = new byte[count];
            File.Seek(Offset + position, SeekOrigin.Begin);
            File.ReadExactly(buffer);
            return buffer;
        }

        public byte[] Decompress(byte[] data, bool compressed, int expected)
        {
            if (!compressed) return data;
            if (Compression != 1)
            {
                var name = Compression switch { 2 => "lzma", 3 => "lzo", 4 => "xz", 5 => "lz4", 6 => "zstd", _ => $"#{Compression}" };
                throw new IOException($"this image uses {name} compression; only gzip images can be read on Windows (mksquashfs -comp gzip)");
            }
            using var output = new MemoryStream(expected);
            using (var z = new ZLibStream(new MemoryStream(data), CompressionMode.Decompress)) z.CopyTo(output);
            return output.ToArray();
        }

        // One metadata block: a 16-bit header (size, bit 15 = stored) and up to 8 KiB of data.
        public (byte[] Data, long Next) Metadata(long position)
        {
            if (metadata.TryGetValue(position, out var cached)) return cached;
            var header = BinaryPrimitives.ReadUInt16LittleEndian(ReadAt(position, 2));
            var size = header & 0x7FFF;
            var block = (Decompress(ReadAt(position + 2, size), (header & 0x8000) == 0, 8192), position + 2 + size);
            metadata[position] = block;
            return block;
        }
    }

    // Reads a run of metadata bytes that may continue into the following blocks.
    sealed class MetaReader(Image image, long block, int offset)
    {
        byte[] data = image.Metadata(block).Data;
        long next = image.Metadata(block).Next;
        int pos = offset;

        public byte[] Read(int count)
        {
            var output = new byte[count];
            var done = 0;
            while (done < count)
            {
                if (pos >= data.Length)
                {
                    (data, var after) = image.Metadata(next);
                    next = after;
                    pos = 0;
                }
                var n = Math.Min(count - done, data.Length - pos);
                Array.Copy(data, pos, output, done, n);
                done += n;
                pos += n;
            }
            return output;
        }

        public ushort U16() => BinaryPrimitives.ReadUInt16LittleEndian(Read(2));
        public uint U32() => BinaryPrimitives.ReadUInt32LittleEndian(Read(4));
        public ulong U64() => BinaryPrimitives.ReadUInt64LittleEndian(Read(8));
    }

    public static void Extract(string path, long offset, string destination)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var image = new Image(file, offset);
        if (file.Length - offset < 96) throw new IOException("not a squashfs image (file too small)");
        var sb = image.ReadAt(0, 96);
        if (BinaryPrimitives.ReadUInt32LittleEndian(sb) != Magic)
            throw new IOException("not a squashfs image (for an AppImage, give -o offset=<payload offset>)");
        if (BinaryPrimitives.ReadUInt16LittleEndian(sb.AsSpan(28)) != 4) throw new IOException("only squashfs 4.0 images are supported");
        image.BlockSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(sb.AsSpan(12));
        image.Fragments = BinaryPrimitives.ReadUInt32LittleEndian(sb.AsSpan(16));
        image.Compression = BinaryPrimitives.ReadUInt16LittleEndian(sb.AsSpan(20));
        var root = BinaryPrimitives.ReadUInt64LittleEndian(sb.AsSpan(32));
        image.InodeTable = (long)BinaryPrimitives.ReadUInt64LittleEndian(sb.AsSpan(64));
        image.DirectoryTable = (long)BinaryPrimitives.ReadUInt64LittleEndian(sb.AsSpan(72));
        image.FragmentTable = (long)BinaryPrimitives.ReadUInt64LittleEndian(sb.AsSpan(80));
        WriteInode(image, root, destination, isRoot: true);
    }

    static void WriteInode(Image image, ulong reference, string target, bool isRoot = false)
    {
        var reader = new MetaReader(image, image.InodeTable + (long)(reference >> 16), (int)(reference & 0xFFFF));
        var type = reader.U16();
        reader.Read(6);                     // mode, uid index, gid index
        var mtime = DateTimeOffset.FromUnixTimeSeconds(reader.U32()).UtcDateTime;
        reader.U32();                       // inode number
        switch (type)
        {
            case 1 or 8:
            {
                uint start, size; ushort offset;
                if (type == 1)
                {
                    start = reader.U32(); reader.U32(); size = reader.U16(); offset = reader.U16();
                }
                else
                {
                    reader.U32(); size = reader.U32(); start = reader.U32(); reader.U32(); reader.U16(); offset = reader.U16();
                }
                if (!isRoot) Directory.CreateDirectory(target);
                foreach (var (name, child) in ListDirectory(image, start, offset, size))
                    WriteInode(image, child, Path.Combine(target, SafeName(name)));
                if (!isRoot) Directory.SetLastWriteTimeUtc(target, mtime);
                break;
            }
            case 2 or 9:
            {
                ulong blocksStart, size;
                uint fragment, fragmentOffset;
                if (type == 2)
                {
                    blocksStart = reader.U32(); fragment = reader.U32(); fragmentOffset = reader.U32(); size = reader.U32();
                }
                else
                {
                    blocksStart = reader.U64(); size = reader.U64(); reader.U64(); reader.U32();
                    fragment = reader.U32(); fragmentOffset = reader.U32(); reader.U32();
                }
                var blockSize = (ulong)image.BlockSize;
                var blocks = fragment == NoFragment ? (size + blockSize - 1) / blockSize : size / blockSize;
                using (var output = File.Create(target))
                {
                    var position = (long)blocksStart;
                    var remaining = size;
                    for (ulong b = 0; b < blocks; b++)
                    {
                        var entry = reader.U32();
                        var expected = (int)Math.Min(blockSize, remaining);
                        var onDisk = (int)(entry & 0xFFFFFF);
                        if (onDisk == 0) output.Write(new byte[expected]);   // sparse block
                        else
                        {
                            var data = image.Decompress(image.ReadAt(position, onDisk), (entry & 0x1000000) == 0, expected);
                            output.Write(data, 0, Math.Min(expected, data.Length));
                            position += onDisk;
                        }
                        remaining -= (ulong)expected;
                    }
                    if (fragment != NoFragment && remaining > 0)
                    {
                        var block = FragmentBlock(image, fragment);
                        output.Write(block, (int)fragmentOffset, (int)remaining);
                    }
                }
                File.SetLastWriteTimeUtc(target, mtime);
                File.SetAttributes(target, FileAttributes.ReadOnly);
                break;
            }
            case 3 or 10:
            {
                reader.U32();
                var linkTarget = Encoding.UTF8.GetString(reader.Read((int)reader.U32())).Replace('/', '\\');
                try { File.CreateSymbolicLink(target, linkTarget); }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    Console.Error.WriteLine($"squashfuse: skipped symlink {target} -> {linkTarget} (Windows needs Developer Mode or admin rights for symlinks)");
                }
                break;
            }
            default:
                // Device nodes, FIFOs and sockets have no Windows file equivalent.
                break;
        }
    }

    static List<(string Name, ulong Inode)> ListDirectory(Image image, uint start, ushort offset, uint size)
    {
        var entries = new List<(string, ulong)>();
        if (size <= 3) return entries;      // the size counts "." and ".." as 3 bytes
        var reader = new MetaReader(image, image.DirectoryTable + start, offset);
        var remaining = (long)size - 3;
        while (remaining > 0)
        {
            var count = reader.U32() + 1;
            var inodeBlock = reader.U32();
            reader.U32();                   // base inode number
            remaining -= 12;
            for (var i = 0; i < count; i++)
            {
                var inodeOffset = reader.U16();
                reader.U16(); reader.U16(); // inode number delta, type
                var nameSize = reader.U16() + 1;
                var name = Encoding.UTF8.GetString(reader.Read(nameSize));
                entries.Add((name, ((ulong)inodeBlock << 16) | inodeOffset));
                remaining -= 8 + nameSize;
            }
        }
        return entries;
    }

    static byte[] FragmentBlock(Image image, uint index)
    {
        // The fragment table is indexed through a list of metadata block pointers, 512 entries each.
        var pointer = (long)BinaryPrimitives.ReadUInt64LittleEndian(image.ReadAt(image.FragmentTable + index / 512 * 8, 8));
        var reader = new MetaReader(image, pointer, (int)(index % 512) * 16);
        var start = (long)reader.U64();
        var size = reader.U32();
        return image.Decompress(image.ReadAt(start, (int)(size & 0xFFFFFF)), (size & 0x1000000) == 0, image.BlockSize);
    }

    // Names Windows can't store get their reserved characters replaced; "..", "/" never leave the target.
    static string SafeName(string name)
    {
        if (name is "." or ".." || name.Contains('/') || name.Contains('\\') || name.Length == 0) throw new IOException($"bad file name in image: {name}");
        var safe = string.Concat(name.Select(c => c is '<' or '>' or ':' or '"' or '|' or '?' or '*' || c < 32 ? '_' : c)).TrimEnd('.', ' ');
        if (safe != name) Console.Error.WriteLine($"squashfuse: renamed '{name}' to '{safe}'");
        return safe.Length == 0 ? "_" : safe;
    }
}
