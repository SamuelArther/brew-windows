using System.Buffers.Binary;
using System.Formats.Tar;
using System.IO.Compression;
using System.Text;

namespace WinTools;

// SIF (Singularity Image Format) container files: a 128-byte header, a table of 585-byte
// descriptors, then the data objects they point at (definition file, labels JSON, signatures and the
// squashfs root file system partition). Layout from github.com/apptainer/sif (little-endian, packed).
sealed class ApptainerSif
{
    public const int HeaderSize = 128, DescriptorSize = 585;

    public sealed class Descriptor
    {
        public int DataType;
        public uint Id, GroupId, LinkedId;
        public long Offset, Size, SizeWithPadding, Created, Modified;
        public string Name;
        public byte[] Extra;
        public int FsType => BinaryPrimitives.ReadInt32LittleEndian(Extra);
        public int PartType => BinaryPrimitives.ReadInt32LittleEndian(Extra.AsSpan(4));
        public string PartArch => ApptainerSif.ArchName(Encoding.ASCII.GetString(Extra, 8, 2));
    }

    public string Path, Version, Arch;
    public byte[] Id;
    public long Created, Modified, DescriptorsFree, DescriptorsTotal, DescriptorsOffset, DescriptorsSize, DataOffset, DataSize;
    public readonly List<Descriptor> Descriptors = [];

    public string IdHex => Convert.ToHexStringLower(Id);

    public static ApptainerSif Open(string path)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var header = new byte[HeaderSize];
        if (file.Read(header) < HeaderSize || Encoding.ASCII.GetString(header, 32, 9) != "SIF_MAGIC")
            throw new IOException($"{path}: not a SIF container file");
        var sif = new ApptainerSif
        {
            Path = path,
            Version = Encoding.ASCII.GetString(header, 42, 2),
            Arch = ArchName(Encoding.ASCII.GetString(header, 45, 2)),
            Id = header[48..64],
        };
        var s = header.AsSpan(64);
        sif.Created = BinaryPrimitives.ReadInt64LittleEndian(s);
        sif.Modified = BinaryPrimitives.ReadInt64LittleEndian(s[8..]);
        sif.DescriptorsFree = BinaryPrimitives.ReadInt64LittleEndian(s[16..]);
        sif.DescriptorsTotal = BinaryPrimitives.ReadInt64LittleEndian(s[24..]);
        sif.DescriptorsOffset = BinaryPrimitives.ReadInt64LittleEndian(s[32..]);
        sif.DescriptorsSize = BinaryPrimitives.ReadInt64LittleEndian(s[40..]);
        sif.DataOffset = BinaryPrimitives.ReadInt64LittleEndian(s[48..]);
        sif.DataSize = BinaryPrimitives.ReadInt64LittleEndian(s[56..]);
        if (sif.DescriptorsTotal is < 0 or > 100000) throw new IOException($"{path}: corrupt SIF header");

        var table = new byte[sif.DescriptorsTotal * DescriptorSize];
        file.Seek(sif.DescriptorsOffset, SeekOrigin.Begin);
        file.ReadExactly(table);
        for (var i = 0; i < sif.DescriptorsTotal; i++)
        {
            var d = table.AsSpan(i * DescriptorSize, DescriptorSize);
            if (d[4] == 0) continue;   // unused slot
            sif.Descriptors.Add(new Descriptor
            {
                DataType = BinaryPrimitives.ReadInt32LittleEndian(d),
                Id = BinaryPrimitives.ReadUInt32LittleEndian(d[5..]),
                GroupId = BinaryPrimitives.ReadUInt32LittleEndian(d[9..]),
                LinkedId = BinaryPrimitives.ReadUInt32LittleEndian(d[13..]),
                Offset = BinaryPrimitives.ReadInt64LittleEndian(d[17..]),
                Size = BinaryPrimitives.ReadInt64LittleEndian(d[25..]),
                SizeWithPadding = BinaryPrimitives.ReadInt64LittleEndian(d[33..]),
                Created = BinaryPrimitives.ReadInt64LittleEndian(d[41..]),
                Modified = BinaryPrimitives.ReadInt64LittleEndian(d[49..]),
                Name = Encoding.UTF8.GetString(d.Slice(73, 128)).TrimEnd('\0'),
                Extra = d.Slice(201, 384).ToArray(),
            });
        }
        return sif;
    }

    public byte[] Read(Descriptor d)
    {
        using var file = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var data = new byte[d.Size];
        file.Seek(d.Offset, SeekOrigin.Begin);
        file.ReadExactly(data);
        return data;
    }

    // The primary system partition: the container's root file system.
    public Descriptor RootFs =>
        Descriptors.FirstOrDefault(d => d.DataType == 0x4004 && d.PartType == 2)
        ?? Descriptors.FirstOrDefault(d => d.DataType == 0x4004 && d.PartType == 1);

    public static string ArchName(string code) => code switch
    {
        "01" => "386", "02" => "amd64", "03" => "arm", "04" => "arm64", "05" => "ppc64", "06" => "ppc64le",
        "07" => "mips", "08" => "mipsle", "09" => "mips64", "10" => "mips64le", "11" => "s390x", "12" => "riscv64",
        _ => "unknown",
    };

    public static string TypeName(Descriptor d) => d.DataType switch
    {
        0x4001 => "Def.FILE",
        0x4002 => "Env.Vars",
        0x4003 => "JSON.Labels",
        0x4004 => $"FS ({FsName(d.FsType)}/{PartName(d.PartType)}/{d.PartArch})",
        0x4005 => "Signature",
        0x4006 => "JSON.Generic",
        0x4007 => "Generic/Raw",
        0x4008 => "Cryptographic Message",
        0x4009 => "SBOM",
        0x400a => "OCI.RootIndex",
        0x400b => "OCI.Blob",
        _ => $"Unknown (0x{d.DataType:x})",
    };

    static string FsName(int t) => t switch { 1 => "Squashfs", 2 => "Ext3", 3 => "Data.Archive", 4 => "Raw.Data", 5 => "Encrypted squashfs", _ => "Unknown" };
    static string PartName(int t) => t switch { 1 => "System", 2 => "*System", 3 => "Data", 4 => "Overlay", _ => "Unknown" };
}

// squashfs 4.0 reader with path lookup and a converter to a POSIX tar stream (modes, owners,
// symlinks, hard links and device nodes intact), which is what `wsl --import` takes.
// gzip (zlib) is the only compressor .NET can decode, and it is mksquashfs's and apptainer's default.
sealed class ApptainerSquash : IDisposable
{
    const uint NoFragment = 0xFFFFFFFF;

    public sealed class Inode
    {
        public int Type, Mode, UidIndex, GidIndex;
        public uint Mtime, Number, Nlink = 1;
        public uint DirStart, DirSize; public int DirOffset;
        public long BlocksStart; public long FileSize; public uint FragIndex = NoFragment, FragOffset; public uint[] Blocks = [];
        public string Target;
        public uint Rdev;
        public bool IsDir => Type is 1 or 8;
        public bool IsFile => Type is 2 or 9;
        public bool IsSymlink => Type is 3 or 10;
    }

    readonly FileStream file;
    readonly long baseOffset;
    readonly int compression, blockSize;
    readonly long inodeTable, dirTable, fragTable, idTable;
    readonly ushort idCount;
    readonly ulong root;
    uint[] ids;
    readonly Dictionary<long, (byte[] Data, long Next)> metadata = [];

    public ApptainerSquash(string path, long offset)
    {
        file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        baseOffset = offset;
        var sb = ReadAt(0, 96);
        if (BinaryPrimitives.ReadUInt32LittleEndian(sb) != 0x73717368) throw new IOException("not a squashfs file system");
        if (BinaryPrimitives.ReadUInt16LittleEndian(sb.AsSpan(28)) != 4) throw new IOException("only squashfs 4.0 is supported");
        blockSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(sb.AsSpan(12));
        compression = BinaryPrimitives.ReadUInt16LittleEndian(sb.AsSpan(20));
        idCount = BinaryPrimitives.ReadUInt16LittleEndian(sb.AsSpan(26));
        root = BinaryPrimitives.ReadUInt64LittleEndian(sb.AsSpan(32));
        idTable = (long)BinaryPrimitives.ReadUInt64LittleEndian(sb.AsSpan(48));
        inodeTable = (long)BinaryPrimitives.ReadUInt64LittleEndian(sb.AsSpan(64));
        dirTable = (long)BinaryPrimitives.ReadUInt64LittleEndian(sb.AsSpan(72));
        fragTable = (long)BinaryPrimitives.ReadUInt64LittleEndian(sb.AsSpan(80));
        if (compression != 1)
        {
            var name = compression switch { 2 => "lzma", 3 => "lzo", 4 => "xz", 5 => "lz4", 6 => "zstd", _ => compression.ToString() };
            throw new IOException($"the image's squashfs is {name}-compressed; only gzip images can be read on Windows (rebuild with mksquashfs -comp gzip)");
        }
    }

    public void Dispose() => file.Dispose();

    byte[] ReadAt(long position, int count)
    {
        var buffer = new byte[count];
        file.Seek(baseOffset + position, SeekOrigin.Begin);
        file.ReadExactly(buffer);
        return buffer;
    }

    byte[] Inflate(byte[] data, int expected)
    {
        using var z = new ZLibStream(new MemoryStream(data), CompressionMode.Decompress);
        var output = new MemoryStream(expected);
        z.CopyTo(output);
        return output.ToArray();
    }

    (byte[] Data, long Next) Metadata(long position)
    {
        if (metadata.TryGetValue(position, out var cached)) return cached;
        var header = BinaryPrimitives.ReadUInt16LittleEndian(ReadAt(position, 2));
        var size = header & 0x7FFF;
        var raw = ReadAt(position + 2, size);
        var block = (header & 0x8000) != 0 ? raw : Inflate(raw, 8192);
        return metadata[position] = (block, position + 2 + size);
    }

    sealed class Reader(ApptainerSquash fs, long block, int offset)
    {
        byte[] current = fs.Metadata(block).Data;
        long next = fs.Metadata(block).Next;

        public byte[] Read(int count)
        {
            var result = new byte[count];
            var done = 0;
            while (done < count)
            {
                if (offset >= current.Length)
                {
                    (current, var after) = fs.Metadata(next);
                    next = after;
                    offset = 0;
                }
                var n = Math.Min(count - done, current.Length - offset);
                Array.Copy(current, offset, result, done, n);
                offset += n; done += n;
            }
            return result;
        }
        public ushort U16() => BinaryPrimitives.ReadUInt16LittleEndian(Read(2));
        public uint U32() => BinaryPrimitives.ReadUInt32LittleEndian(Read(4));
        public ulong U64() => BinaryPrimitives.ReadUInt64LittleEndian(Read(8));
    }

    public Inode Root => ReadInode(root);

    public Inode ReadInode(ulong reference)
    {
        var r = new Reader(this, inodeTable + (long)(reference >> 16), (int)(reference & 0xFFFF));
        var n = new Inode { Type = r.U16(), Mode = r.U16(), UidIndex = r.U16(), GidIndex = r.U16(), Mtime = r.U32(), Number = r.U32() };
        switch (n.Type)
        {
            case 1:
                n.DirStart = r.U32(); n.Nlink = r.U32(); n.DirSize = r.U16(); n.DirOffset = r.U16(); r.U32();
                break;
            case 8:
                n.Nlink = r.U32(); n.DirSize = r.U32(); n.DirStart = r.U32(); r.U32(); r.U16(); n.DirOffset = r.U16(); r.U32();
                break;
            case 2:
                n.BlocksStart = r.U32(); n.FragIndex = r.U32(); n.FragOffset = r.U32(); n.FileSize = r.U32();
                n.Blocks = ReadBlockList(r, n);
                break;
            case 9:
                n.BlocksStart = (long)r.U64(); n.FileSize = (long)r.U64(); r.U64(); n.Nlink = r.U32();
                n.FragIndex = r.U32(); n.FragOffset = r.U32(); r.U32();
                n.Blocks = ReadBlockList(r, n);
                break;
            case 3 or 10:
                n.Nlink = r.U32();
                n.Target = Encoding.UTF8.GetString(r.Read((int)r.U32()));
                break;
            case 4 or 5 or 11 or 12:
                n.Nlink = r.U32(); n.Rdev = r.U32();
                break;
            default:
                n.Nlink = r.U32();
                break;
        }
        return n;
    }

    uint[] ReadBlockList(Reader r, Inode n)
    {
        var count = n.FragIndex == NoFragment ? (n.FileSize + blockSize - 1) / blockSize : n.FileSize / blockSize;
        var blocks = new uint[count];
        for (var i = 0; i < count; i++) blocks[i] = r.U32();
        return blocks;
    }

    public List<(string Name, ulong Reference)> List(Inode dir)
    {
        var list = new List<(string, ulong)>();
        if (dir.DirSize <= 3) return list;
        var r = new Reader(this, dirTable + dir.DirStart, dir.DirOffset);
        long remaining = dir.DirSize - 3;
        while (remaining > 0)
        {
            var count = r.U32() + 1;
            var start = r.U32();
            r.U32();
            remaining -= 12;
            for (var i = 0; i < count && remaining > 0; i++)
            {
                var offset = r.U16();
                r.U16(); r.U16();
                var size = r.U16() + 1;
                var name = Encoding.UTF8.GetString(r.Read(size));
                remaining -= 8 + size;
                list.Add((name, ((ulong)start << 16) | offset));
            }
        }
        return list;
    }

    // Looks a path up from the root, following symlinks (absolute ones stay inside the image).
    public Inode Lookup(string path, int depth = 0)
    {
        if (depth > 16) return null;
        var node = Root;
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var walked = new List<string>();
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i] == ".") continue;
            if (parts[i] == "..") { if (walked.Count > 0) walked.RemoveAt(walked.Count - 1); node = Lookup(string.Join('/', walked), depth + 1); continue; }
            if (node == null || !node.IsDir) return null;
            var entry = List(node).FirstOrDefault(e => e.Name == parts[i]);
            if (entry.Name == null) return null;
            node = ReadInode(entry.Reference);
            if (node.IsSymlink)
            {
                var target = node.Target.StartsWith('/') ? node.Target : string.Join('/', walked) + "/" + node.Target;
                node = Lookup(target, depth + 1);
            }
            walked.Add(parts[i]);
        }
        return node;
    }

    public byte[] ReadFile(string path)
    {
        var node = Lookup(path);
        if (node == null || !node.IsFile) return null;
        var output = new MemoryStream();
        CopyData(node, output);
        return output.ToArray();
    }

    public void CopyData(Inode n, Stream output)
    {
        var position = n.BlocksStart;
        var remaining = n.FileSize;
        foreach (var entry in n.Blocks)
        {
            var size = (int)(entry & 0xFFFFFF);
            var expected = (int)Math.Min(blockSize, remaining);
            if (size == 0) output.Write(new byte[expected]);   // sparse block
            else
            {
                var raw = ReadAt(position, size);
                position += size;
                var data = (entry & 0x1000000) != 0 ? raw : Inflate(raw, blockSize);
                output.Write(data, 0, Math.Min(expected, data.Length));
            }
            remaining -= expected;
        }
        if (remaining > 0 && n.FragIndex != NoFragment)
        {
            var table = BinaryPrimitives.ReadUInt64LittleEndian(ReadAt(fragTable + 8 * (n.FragIndex / 512), 8));
            var r = new Reader(this, (long)table, (int)(n.FragIndex % 512) * 16);
            var start = (long)r.U64();
            var size = r.U32();
            var raw = ReadAt(start, (int)(size & 0xFFFFFF));
            var data = (size & 0x1000000) != 0 ? raw : Inflate(raw, blockSize);
            output.Write(data, (int)n.FragOffset, (int)remaining);
        }
    }

    uint Id(int index)
    {
        if (ids == null)
        {
            ids = new uint[idCount];
            for (var i = 0; i < idCount; i += 2048)
            {
                var block = BinaryPrimitives.ReadUInt64LittleEndian(ReadAt(idTable + 8 * (i / 2048), 8));
                var r = new Reader(this, (long)block, 0);
                for (var j = i; j < Math.Min(idCount, i + 2048); j++) ids[j] = r.U32();
            }
        }
        return index < ids.Length ? ids[index] : 0;
    }

    // The whole tree as a tar stream.
    public void WriteTar(TarWriter tar)
    {
        var links = new Dictionary<uint, string>();
        WriteTree(tar, Root, "", links);
    }

    void WriteTree(TarWriter tar, Inode dir, string prefix, Dictionary<uint, string> links)
    {
        foreach (var (name, reference) in List(dir))
        {
            var n = ReadInode(reference);
            var path = prefix + name;
            PaxTarEntry entry;
            if (n.IsDir) entry = new PaxTarEntry(TarEntryType.Directory, path + "/");
            else if (n.Nlink > 1 && !n.IsDir && links.TryGetValue(n.Number, out var first))
                entry = new PaxTarEntry(TarEntryType.HardLink, path) { LinkName = first };
            else
            {
                if (n.Nlink > 1) links[n.Number] = path;
                switch (n.Type)
                {
                    case 2 or 9:
                        entry = new PaxTarEntry(TarEntryType.RegularFile, path);
                        Stream data;
                        if (n.FileSize > 64 << 20)
                        {
                            var temp = Path.GetTempFileName();
                            data = new FileStream(temp, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1 << 16, FileOptions.DeleteOnClose);
                        }
                        else data = new MemoryStream((int)n.FileSize);
                        CopyData(n, data);
                        data.Position = 0;
                        entry.DataStream = data;
                        break;
                    case 3 or 10: entry = new PaxTarEntry(TarEntryType.SymbolicLink, path) { LinkName = n.Target }; break;
                    case 4 or 11 or 5 or 12:
                        entry = new PaxTarEntry(n.Type is 4 or 11 ? TarEntryType.BlockDevice : TarEntryType.CharacterDevice, path)
                        {
                            DeviceMajor = (int)((n.Rdev >> 8) & 0xFFF),
                            DeviceMinor = (int)((n.Rdev & 0xFF) | ((n.Rdev >> 12) & 0xFFF00)),
                        };
                        break;
                    case 6 or 13: entry = new PaxTarEntry(TarEntryType.Fifo, path); break;
                    default: continue;   // sockets aren't archived
                }
            }
            entry.Mode = (UnixFileMode)(n.Mode & 0xFFF);
            entry.Uid = (int)Id(n.UidIndex);
            entry.Gid = (int)Id(n.GidIndex);
            entry.ModificationTime = DateTimeOffset.FromUnixTimeSeconds(n.Mtime);
            tar.WriteEntry(entry);
            entry.DataStream?.Dispose();
            if (n.IsDir) WriteTree(tar, n, path + "/", links);
        }
    }
}
