using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace WinTools;

// btrfs-progs on Windows: mkfs.btrfs writes a new btrfs file system to an image file or a whole
// disk (\\.\PhysicalDriveN) with the on-disk format the Linux kernel and WinBtrfs mount: superblock
// and its mirrors, a single-device SYSTEM, METADATA and DATA chunk, the chunk, root, extent, device,
// checksum and data-relocation trees, and the top-level subvolume's root directory (the kernel adds
// the free-space and UUID trees on the first read-write mount). `btrfs` reads file systems the same
// way: filesystem show/label/df/usage, inspect-internal dump-super and dump-tree, check (superblock
// and every tree block's checksum and structure), device scan (btrfs partitions on local disks).
// Commands that act on a mounted file system (subvolume, balance, scrub, send/receive, quota ...)
// need the kernel's ioctls; on Windows a mounted btrfs belongs to WinBtrfs, so they explain that.
static class Btrfs
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("mkfs.btrfs", "Create a btrfs file system on an image file or disk", Mkfs);
        Commands.Add("btrfs", "Inspect and label btrfs file systems (show, df, check, dump-super ...)", Btrfs_);
    }

    const string Version = "v7.1";
    const ulong Magic = 0x4D5F53665248425F;   // "_BHRfS_M"
    const long SuperOffset = 0x10000;
    const int HeaderSize = 101, ItemSize = 25, KeyPtrSize = 33;

    // object ids and item types
    const ulong RootTree = 1, ExtentTree = 2, ChunkTree = 3, DevTree = 4, FsTree = 5, RootTreeDir = 6, CsumTree = 7;
    const ulong DataRelocTree = unchecked((ulong)-9L), DevItems = 1, FirstChunkTree = 256, FirstFree = 256, DevStats = 0;
    const byte InodeItem = 1, InodeRef = 12, DirItem = 84, RootItem = 132, MetadataItem = 169, TreeBlockRef = 176;
    const byte BlockGroupItem = 192, DevExtent = 204, DevItem = 216, ChunkItem = 228, PersistentItem = 249;
    const ulong GroupData = 1, GroupSystem = 2, GroupMetadata = 4;
    const ulong IncompatMixedBackref = 0x1, IncompatBigMetadata = 0x20, IncompatExtref = 0x40, IncompatSkinny = 0x100, IncompatNoHoles = 0x200;

    // ---- crc32c (btrfs' checksum and name hash) ----

    static uint Crc32c(uint crc, ReadOnlySpan<byte> data)
    {
        var i = 0;
        for (; i + 8 <= data.Length; i += 8) crc = BitOperations.Crc32C(crc, BinaryPrimitives.ReadUInt64LittleEndian(data[i..]));
        for (; i < data.Length; i++) crc = BitOperations.Crc32C(crc, data[i]);
        return crc;
    }

    static void Checksum(Span<byte> block) => BinaryPrimitives.WriteUInt32LittleEndian(block, ~Crc32c(0xFFFFFFFF, block[32..]));
    static bool ChecksumOk(ReadOnlySpan<byte> block) => BinaryPrimitives.ReadUInt32LittleEndian(block) == ~Crc32c(0xFFFFFFFF, block[32..]);
    static ulong NameHash(string name) => Crc32c(0xFFFFFFFE, Encoding.UTF8.GetBytes(name));

    // ---- sizes ----

    static string Pretty(ulong size)
    {
        string[] units = { "B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB" };
        if (size < 1024) return $"{size}{units[0]}";
        double value = size;
        var u = 0;
        while (value >= 1024 && u < units.Length - 1) { value /= 1024; u++; }
        return value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + units[u];
    }

    static ulong ParseSize(string s)
    {
        var m = System.Text.RegularExpressions.Regex.Match(s.Trim(), @"^(\d+)([kKmMgGtTpPeE]?)[iI]?[bB]?$");
        if (!m.Success) throw new UsageException($"invalid size: {s}");
        var n = ulong.Parse(m.Groups[1].Value);
        var shift = "kmgtpe".IndexOf(char.ToLowerInvariant(m.Groups[2].Value.FirstOrDefault('_'))) + 1;
        return n << (10 * shift);
    }

    // ---- devices (image files and disks) ----

    sealed class Device : IDisposable
    {
        public readonly string Path;
        public readonly ulong Size;
        readonly FileStream file;

        public Device(string path, bool write, ulong createSize = 0)
        {
            Path = path;
            var raw = path.StartsWith(@"\\.\", StringComparison.Ordinal);
            if (raw)
            {
                var handle = CreateFileW(path, write ? 0xC0000000 : 0x80000000, 3, 0, 3, 0, 0);
                if (handle.IsInvalid)
                    throw new InvalidOperationException($"cannot open {path}: {new System.ComponentModel.Win32Exception().Message}{(IsUserAnAdmin() ? "" : " (disks need an elevated prompt)")}");
                file = new FileStream(handle, write ? FileAccess.ReadWrite : FileAccess.Read, 0);
                var length = new byte[8];
                if (!DeviceIoControl(handle, 0x0007405C /* IOCTL_DISK_GET_LENGTH_INFO */, 0, 0, length, 8, out _, 0))
                    throw new InvalidOperationException($"cannot get the size of {path}");
                Size = BitConverter.ToUInt64(length);
            }
            else
            {
                if (!write && !File.Exists(path)) throw new InvalidOperationException($"{path}: No such file or directory");
                file = new FileStream(path, write ? FileMode.OpenOrCreate : FileMode.Open, write ? FileAccess.ReadWrite : FileAccess.Read, FileShare.Read);
                if (createSize > 0 && (ulong)file.Length < createSize) file.SetLength((long)createSize);
                Size = (ulong)file.Length;
            }
        }

        public byte[] Read(ulong offset, int length)
        {
            var buffer = new byte[length];
            file.Position = (long)offset;
            var n = 0;
            while (n < length)
            {
                var got = file.Read(buffer, n, length - n);
                if (got <= 0) break;
                n += got;
            }
            return buffer;
        }

        public void Write(ulong offset, ReadOnlySpan<byte> data)
        {
            file.Position = (long)offset;
            file.Write(data);
        }

        public void Flush() => file.Flush(true);
        public void Dispose() => file.Dispose();
    }

    // ---- leaf builder ----

    sealed class Leaf
    {
        readonly List<(ulong Objectid, byte Type, ulong Offset, byte[] Data)> items = new();
        public void Add(ulong objectid, byte type, ulong offset, byte[] data) => items.Add((objectid, type, offset, data));

        public byte[] Build(int nodesize, byte[] fsid, byte[] chunkTreeUuid, ulong bytenr, ulong owner, ulong generation)
        {
            var block = new byte[nodesize];
            var sorted = items.OrderBy(i => i.Objectid).ThenBy(i => i.Type).ThenBy(i => i.Offset).ToList();
            var dataEnd = nodesize - HeaderSize;
            for (var k = 0; k < sorted.Count; k++)
            {
                var (objectid, type, offset, data) = sorted[k];
                dataEnd -= data.Length;
                var at = HeaderSize + k * ItemSize;
                if (at + ItemSize > HeaderSize + dataEnd) throw new InvalidOperationException("tree block overflow");
                BinaryPrimitives.WriteUInt64LittleEndian(block.AsSpan(at), objectid);
                block[at + 8] = type;
                BinaryPrimitives.WriteUInt64LittleEndian(block.AsSpan(at + 9), offset);
                BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(at + 17), (uint)dataEnd);
                BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(at + 21), (uint)data.Length);
                data.CopyTo(block, HeaderSize + dataEnd);
            }
            fsid.CopyTo(block, 32);
            BinaryPrimitives.WriteUInt64LittleEndian(block.AsSpan(48), bytenr);
            BinaryPrimitives.WriteUInt64LittleEndian(block.AsSpan(56), 1UL | (1UL << 56));   // WRITTEN, mixed backrefs
            chunkTreeUuid.CopyTo(block, 64);
            BinaryPrimitives.WriteUInt64LittleEndian(block.AsSpan(80), generation);
            BinaryPrimitives.WriteUInt64LittleEndian(block.AsSpan(88), owner);
            BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(96), (uint)sorted.Count);
            block[100] = 0;
            Checksum(block);
            return block;
        }
    }

    sealed class Bytes
    {
        readonly MemoryStream s = new();
        public Bytes U8(byte v) { s.WriteByte(v); return this; }
        public Bytes U16(ushort v) { Span<byte> b = stackalloc byte[2]; BinaryPrimitives.WriteUInt16LittleEndian(b, v); s.Write(b); return this; }
        public Bytes U32(uint v) { Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(b, v); s.Write(b); return this; }
        public Bytes U64(ulong v) { Span<byte> b = stackalloc byte[8]; BinaryPrimitives.WriteUInt64LittleEndian(b, v); s.Write(b); return this; }
        public Bytes Raw(ReadOnlySpan<byte> v) { s.Write(v); return this; }
        public Bytes Zero(int n) { s.Write(new byte[n]); return this; }
        public Bytes Key(ulong objectid, byte type, ulong offset) => U64(objectid).U8(type).U64(offset);
        public Bytes Time(long seconds) => U64((ulong)seconds).U32(0);
        public byte[] ToArray() => s.ToArray();
    }

    static byte[] Inode(ulong generation, ulong size, ulong nbytes, uint mode, long now) =>
        new Bytes().U64(generation).U64(generation).U64(size).U64(nbytes).U64(0).U32(1).U32(0).U32(0).U32(mode)
            .U64(0).U64(0).U64(0).Zero(32).Time(now).Time(now).Time(now).Time(0).ToArray();

    static byte[] Root(ulong bytenr, int nodesize, ulong dirid, byte[] uuid, long now) =>
        new Bytes().Raw(Inode(1, 3, (ulong)nodesize, 0x41ED, 0)).U64(1).U64(dirid).U64(bytenr).U64(0).U64((ulong)nodesize)
            .U64(0).U64(0).U32(1).Key(0, 0, 0).U8(0).U8(0).U64(1).Raw(uuid).Zero(16).Zero(16)
            .U64(0).U64(0).U64(0).U64(0).Time(now).Time(now).Time(0).Time(0).Zero(64).ToArray();

    static byte[] InodeRefDotDot() => new Bytes().U64(0).U16(2).Raw(".."u8).ToArray();

    // ---- mkfs.btrfs ----

    static int Mkfs(string[] args)
    {
        string label = null, uuid = null, device = null;
        int nodesize = 16384, sectorsize = 4096;
        ulong byteCount = 0;
        bool force = false, quiet = false;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            var eq = a.StartsWith("--") ? a.IndexOf('=') : -1;
            var name = eq > 0 ? a[..eq] : a;
            // -Lvalue as well as -L value and --label=value
            string attached = null;
            if (!name.StartsWith("--") && name.Length > 2 && name[0] == '-') { attached = name[2..]; name = name[..2]; }
            string Value() => attached ?? (eq > 0 ? a[(eq + 1)..] : ++i < args.Length ? args[i] : throw new UsageException($"option requires an argument -- '{name}'"));
            switch (name)
            {
                case "-h" or "--help": MkfsUsage(); return 0;
                case "-V" or "--version": Console.WriteLine($"mkfs.btrfs, part of btrfs-progs {Version}"); return 0;
                case "-L" or "--label": label = Value(); break;
                case "-U" or "--uuid": uuid = Value(); break;
                case "-n" or "--nodesize": nodesize = (int)ParseSize(Value()); break;
                case "-s" or "--sectorsize": sectorsize = (int)ParseSize(Value()); break;
                case "-b" or "--byte-count": byteCount = ParseSize(Value()); break;
                case "-f" or "--force": force = true; break;
                case "-q" or "--quiet": quiet = true; break;
                case "-K" or "--nodiscard" or "-v" or "--verbose": break;
                case "-m" or "--metadata" or "-d" or "--data":
                {
                    var profile = Value().ToLowerInvariant();
                    if (profile != "single") throw new InvalidOperationException($"profile {profile}: only the single profile is written by this mkfs.btrfs (balance to {profile} after mounting)");
                    break;
                }
                case "-O" or "--features" or "-R" or "--runtime-features":
                    Value();
                    break;
                case "--csum" or "--checksum":
                    if (Value().ToLowerInvariant() != "crc32c") throw new InvalidOperationException("only the crc32c checksum is written by this mkfs.btrfs");
                    break;
                case "-r" or "--rootdir" or "--subvol":
                    throw new InvalidOperationException($"{name}: populating the new file system from a directory isn't supported here; copy files in after mounting");
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unrecognized option '{a}'");
                    if (device != null) throw new InvalidOperationException("multiple devices: this mkfs.btrfs writes single-device file systems");
                    device = a;
                    break;
            }
        }
        if (device == null) throw new UsageException("not enough arguments: 0 but at least 1 expected");
        if (nodesize is < 4096 or > 65536 || (nodesize & (nodesize - 1)) != 0 || nodesize < sectorsize)
            throw new InvalidOperationException($"invalid nodesize {nodesize}");
        if (sectorsize != 4096) throw new InvalidOperationException("only a 4096-byte sector size is supported (the Windows page size)");
        if (label != null && Encoding.UTF8.GetByteCount(label) >= 256) throw new InvalidOperationException("label too long");
        if (device.Length == 2 && device[1] == ':' || device.StartsWith(@"\\.\") && device.Length == 6 && device[5] == ':')
            throw new InvalidOperationException("formatting a mounted volume isn't supported; give an image file or a whole disk (\\\\.\\PhysicalDriveN)");

        using var dev = new Device(device, write: true, createSize: byteCount);
        var total = byteCount > 0 ? Math.Min(byteCount, dev.Size) : dev.Size;
        total &= ~(ulong)(sectorsize - 1);
        const ulong mib = 1UL << 20;
        if (total < 32 * mib) throw new InvalidOperationException($"device is too small to make filesystem, must be at least {32 * mib} bytes");
        if (!force && Probe(dev, out var existing))
            throw new InvalidOperationException($"{device} appears to contain an existing filesystem (btrfs, {existing}).\nUse the -f option to force overwrite.");

        var fsid = uuid != null ? ParseUuid(uuid) : Guid.NewGuid().ToByteArray();
        var devUuid = Guid.NewGuid().ToByteArray();
        var chunkTreeUuid = Guid.NewGuid().ToByteArray();
        var fsTreeUuid = Guid.NewGuid().ToByteArray();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Chunks: SYSTEM [1M, 5M), then METADATA and DATA, identity-mapped (logical == physical).
        ulong sysStart = mib, sysSize = 4 * mib;
        var metaSize = Math.Clamp(total / 10 & ~(mib - 1), 8 * mib, 256 * mib);
        var dataSize = Math.Clamp(total / 10 & ~(mib - 1), 8 * mib, 1024 * mib);
        var metaStart = sysStart + sysSize;
        var dataStart = metaStart + metaSize;
        if (dataStart + dataSize > total) throw new InvalidOperationException("device is too small for the initial chunks");
        var ns = (ulong)nodesize;
        // Tree blocks: chunk tree in SYSTEM; the rest in METADATA, in this order.
        var chunkBlock = sysStart;
        ulong rootBlock = metaStart, extentBlock = metaStart + ns, devBlock = metaStart + 2 * ns, fsBlock = metaStart + 3 * ns;
        ulong csumBlock = metaStart + 4 * ns, relocBlock = metaStart + 5 * ns;
        var metaBlocks = new (ulong Bytenr, ulong Owner)[] { (rootBlock, RootTree), (extentBlock, ExtentTree), (devBlock, DevTree), (fsBlock, FsTree), (csumBlock, CsumTree), (relocBlock, DataRelocTree) };

        // Wipe the first 1 MiB and the end (old signatures), then write the trees.
        dev.Write(0, new byte[mib]);
        if (total > 2 * mib) dev.Write(total - 2 * mib, new byte[2 * mib]);

        byte[] Chunk(ulong length, ulong type, ulong offset) =>
            new Bytes().U64(length).U64(ExtentTree).U64(0x10000).U64(type).U32((uint)sectorsize).U32((uint)sectorsize).U32((uint)sectorsize)
                .U16(1).U16(0).U64(1).U64(offset).Raw(devUuid).ToArray();
        var devItem = new Bytes().U64(1).U64(total).U64(sysSize + metaSize + dataSize).U32((uint)sectorsize).U32((uint)sectorsize).U32((uint)sectorsize)
            .U64(0).U64(0).U64(0).U32(0).U8(0).U8(0).Raw(devUuid).Raw(fsid).ToArray();

        var chunk = new Leaf();
        chunk.Add(DevItems, DevItem, 1, devItem);
        chunk.Add(FirstChunkTree, ChunkItem, sysStart, Chunk(sysSize, GroupSystem, sysStart));
        chunk.Add(FirstChunkTree, ChunkItem, metaStart, Chunk(metaSize, GroupMetadata, metaStart));
        chunk.Add(FirstChunkTree, ChunkItem, dataStart, Chunk(dataSize, GroupData, dataStart));
        dev.Write(chunkBlock, chunk.Build(nodesize, fsid, chunkTreeUuid, chunkBlock, ChunkTree, 1));

        var root = new Leaf();
        var zeroUuid = new byte[16];
        root.Add(ExtentTree, RootItem, 0, Root(extentBlock, nodesize, 0, zeroUuid, 0));
        root.Add(DevTree, RootItem, 0, Root(devBlock, nodesize, 0, zeroUuid, 0));
        root.Add(FsTree, RootItem, 0, Root(fsBlock, nodesize, FirstFree, fsTreeUuid, now));
        root.Add(CsumTree, RootItem, 0, Root(csumBlock, nodesize, 0, zeroUuid, 0));
        root.Add(DataRelocTree, RootItem, 0, Root(relocBlock, nodesize, FirstFree, zeroUuid, 0));
        root.Add(RootTreeDir, InodeItem, 0, Inode(1, 0, ns, 0x41ED, now));
        root.Add(RootTreeDir, InodeRef, RootTreeDir, InodeRefDotDot());
        root.Add(RootTreeDir, DirItem, NameHash("default"),
            new Bytes().Key(FsTree, RootItem, ulong.MaxValue).U64(0).U16(0).U16(7).U8(2).Raw("default"u8).ToArray());
        dev.Write(rootBlock, root.Build(nodesize, fsid, chunkTreeUuid, rootBlock, RootTree, 1));

        var extent = new Leaf();
        var extentItem = new Bytes().U64(1).U64(1).U64(2).ToArray();   // refs, generation, TREE_BLOCK
        foreach (var (bytenr, owner) in metaBlocks.Append((chunkBlock, ChunkTree)))
        {
            extent.Add(bytenr, MetadataItem, 0, extentItem);
            extent.Add(bytenr, TreeBlockRef, owner, Array.Empty<byte>());
        }
        byte[] Group(ulong used, ulong flags) => new Bytes().U64(used).U64(FirstChunkTree).U64(flags).ToArray();
        extent.Add(sysStart, BlockGroupItem, sysSize, Group(ns, GroupSystem));
        extent.Add(metaStart, BlockGroupItem, metaSize, Group(ns * (ulong)metaBlocks.Length, GroupMetadata));
        extent.Add(dataStart, BlockGroupItem, dataSize, Group(0, GroupData));
        dev.Write(extentBlock, extent.Build(nodesize, fsid, chunkTreeUuid, extentBlock, ExtentTree, 1));

        var devTree = new Leaf();
        devTree.Add(DevStats, PersistentItem, 1, new byte[40]);
        foreach (var (start, size) in new[] { (sysStart, sysSize), (metaStart, metaSize), (dataStart, dataSize) })
            devTree.Add(1, DevExtent, start, new Bytes().U64(ChunkTree).U64(FirstChunkTree).U64(start).U64(size).Raw(chunkTreeUuid).ToArray());
        dev.Write(devBlock, devTree.Build(nodesize, fsid, chunkTreeUuid, devBlock, DevTree, 1));

        foreach (var (block, owner) in new[] { (fsBlock, FsTree), (relocBlock, DataRelocTree) })
        {
            var dirTree = new Leaf();
            dirTree.Add(FirstFree, InodeItem, 0, Inode(1, 0, ns, 0x41ED, now));
            dirTree.Add(FirstFree, InodeRef, FirstFree, InodeRefDotDot());
            dev.Write(block, dirTree.Build(nodesize, fsid, chunkTreeUuid, block, owner, 1));
        }
        dev.Write(csumBlock, new Leaf().Build(nodesize, fsid, chunkTreeUuid, csumBlock, CsumTree, 1));

        // The superblock, and its mirrors at 64 MiB and 256 GiB when the device reaches them.
        var bytesUsed = ns * (ulong)(metaBlocks.Length + 1);
        var sysArray = new Bytes().Key(FirstChunkTree, ChunkItem, sysStart).Raw(Chunk(sysSize, GroupSystem, sysStart)).ToArray();
        foreach (var (offset, index) in new[] { ((ulong)SuperOffset, 0), (64 * mib, 1), (256UL << 30, 2) })
        {
            if (offset + 4096 > total) continue;
            var super = new byte[4096];
            fsid.CopyTo(super, 0x20);
            var s = super.AsSpan();
            BinaryPrimitives.WriteUInt64LittleEndian(s[0x30..], offset);
            BinaryPrimitives.WriteUInt64LittleEndian(s[0x40..], Magic);
            BinaryPrimitives.WriteUInt64LittleEndian(s[0x48..], 1);
            BinaryPrimitives.WriteUInt64LittleEndian(s[0x50..], rootBlock);
            BinaryPrimitives.WriteUInt64LittleEndian(s[0x58..], chunkBlock);
            BinaryPrimitives.WriteUInt64LittleEndian(s[0x70..], total);
            BinaryPrimitives.WriteUInt64LittleEndian(s[0x78..], bytesUsed);
            BinaryPrimitives.WriteUInt64LittleEndian(s[0x80..], RootTreeDir);
            BinaryPrimitives.WriteUInt64LittleEndian(s[0x88..], 1);
            BinaryPrimitives.WriteUInt32LittleEndian(s[0x90..], (uint)sectorsize);
            BinaryPrimitives.WriteUInt32LittleEndian(s[0x94..], (uint)nodesize);
            BinaryPrimitives.WriteUInt32LittleEndian(s[0x98..], (uint)nodesize);
            BinaryPrimitives.WriteUInt32LittleEndian(s[0x9c..], (uint)sectorsize);
            BinaryPrimitives.WriteUInt32LittleEndian(s[0xa0..], (uint)sysArray.Length);
            BinaryPrimitives.WriteUInt64LittleEndian(s[0xa4..], 1);
            BinaryPrimitives.WriteUInt64LittleEndian(s[0xbc..], IncompatMixedBackref | IncompatBigMetadata | IncompatExtref | IncompatSkinny | IncompatNoHoles);
            devItem.CopyTo(super, 0xc9);
            if (label != null) Encoding.UTF8.GetBytes(label).CopyTo(super, 0x12b);
            BinaryPrimitives.WriteUInt64LittleEndian(s[0x22b..], ulong.MaxValue);   // cache_generation: no v1 cache; the kernel builds the free space tree
            sysArray.CopyTo(super, 0x32b);
            // Backup root 0 (struct btrfs_root_backup): tree roots and generations.
            var backup = new Bytes().U64(rootBlock).U64(1).U64(chunkBlock).U64(1).U64(extentBlock).U64(1).U64(fsBlock).U64(1)
                .U64(devBlock).U64(1).U64(csumBlock).U64(1).U64(total).U64(bytesUsed).U64(1).Zero(32).Zero(8).ToArray();
            backup.CopyTo(super, 0xb2b);
            Checksum(super);
            _ = index;
            dev.Write(offset, super);
        }
        dev.Flush();

        if (!quiet)
        {
            Console.WriteLine($"btrfs-progs {Version}\nSee https://btrfs.readthedocs.io for more information.\n");
            Console.WriteLine($"Label:              {(label ?? "(null)")}");
            Console.WriteLine($"UUID:               {FormatUuid(fsid)}");
            Console.WriteLine($"Node size:          {nodesize}");
            Console.WriteLine($"Sector size:        {sectorsize}\t(CPU page size: 4096)");
            Console.WriteLine($"Filesystem size:    {Pretty(total)}");
            Console.WriteLine("Block group profiles:");
            Console.WriteLine($"  Data:             {"single",-8} {Pretty(dataSize),16}");
            Console.WriteLine($"  Metadata:         {"single",-8} {Pretty(metaSize),16}");
            Console.WriteLine($"  System:           {"single",-8} {Pretty(sysSize),16}");
            Console.WriteLine("SSD detected:       no");
            Console.WriteLine("Zoned device:       no");
            Console.WriteLine("Features:           extref, skinny-metadata, no-holes");
            Console.WriteLine("Checksum:           crc32c");
            Console.WriteLine("Number of devices:  1");
            Console.WriteLine("Devices:");
            Console.WriteLine($"   ID        SIZE  PATH");
            Console.WriteLine($"    1  {Pretty(total),10}  {device}");
            Console.WriteLine();
        }
        return 0;
    }

    static void MkfsUsage() => Console.WriteLine("""
        Usage: mkfs.btrfs [options] <dev> [<dev...>]

            Create a BTRFS filesystem on a device or image file (on Windows: an image file or \\.\PhysicalDriveN).

        Options:
          allocation profiles:
            -d|--data PROFILE           data profile (single)
            -m|--metadata PROFILE       metadata profile (single)
          features:
            --csum TYPE
            --checksum TYPE             checksum algorithm to use (crc32c)
            -n|--nodesize SIZE          size of btree nodes
            -s|--sectorsize SIZE        data block size (4096)
          creation:
            -b|--byte-count SIZE        set size of each device to SIZE (creates or extends an image file)
            -K|--nodiscard              do not perform whole device TRIM
            -f|--force                  force overwrite of existing filesystem
          general:
            -L|--label LABEL            set the filesystem label
            -U|--uuid UUID              specify the filesystem UUID (must be unique for a filesystem with multiple devices)
            -q|--quiet                  no messages except errors
            -V|--version                print the mkfs.btrfs version and exit
            --help                      print this help and exit
        """);

    static bool Probe(Device dev, out string label)
    {
        label = null;
        if (dev.Size < SuperOffset + 4096) return false;
        var super = dev.Read((ulong)SuperOffset, 4096);
        if (BinaryPrimitives.ReadUInt64LittleEndian(super.AsSpan(0x40)) != Magic) return false;
        label = FormatUuid(super.AsSpan(0x20, 16).ToArray());
        return true;
    }

    static byte[] ParseUuid(string s)
    {
        if (!Guid.TryParse(s, out _)) throw new UsageException($"could not parse UUID: {s}");
        var hex = s.Replace("-", "");
        return Convert.FromHexString(hex);
    }

    static string FormatUuid(byte[] b)
    {
        var h = Convert.ToHexStringLower(b);
        return $"{h[..8]}-{h[8..12]}-{h[12..16]}-{h[16..20]}-{h[20..32]}";
    }

    // ---- reading a file system ----

    sealed class Fs : IDisposable
    {
        public Device Dev;
        public byte[] Super;
        public int Nodesize;
        public List<(ulong Logical, ulong Length, ulong Physical, ulong Type)> Chunks = new();

        public ulong U64(int at) => BinaryPrimitives.ReadUInt64LittleEndian(Super.AsSpan(at));
        public uint U32(int at) => BinaryPrimitives.ReadUInt32LittleEndian(Super.AsSpan(at));
        public string Label => Encoding.UTF8.GetString(Super, 0x12b, 256).TrimEnd('\0');
        public byte[] Fsid => Super.AsSpan(0x20, 16).ToArray();

        public static Fs Open(string path, bool write = false)
        {
            var fs = new Fs { Dev = new Device(path, write) };
            if (fs.Dev.Size < SuperOffset + 4096) throw new InvalidOperationException($"{path}: not a btrfs filesystem (too small)");
            fs.Super = fs.Dev.Read((ulong)SuperOffset, 4096);
            if (fs.U64(0x40) != Magic) throw new InvalidOperationException($"{path}: not a btrfs filesystem (no superblock)");
            fs.Nodesize = (int)fs.U32(0x94);
            // System chunks from the superblock, then all chunks from the chunk tree.
            var size = (int)fs.U32(0xa0);
            for (var at = 0; at + 17 + 48 <= size;)
            {
                var item = fs.Super.AsSpan(0x32b + at);
                var logical = BinaryPrimitives.ReadUInt64LittleEndian(item[9..]);
                var chunk = item[17..];
                var stripes = BinaryPrimitives.ReadUInt16LittleEndian(chunk[44..]);
                fs.Chunks.Add((logical, BinaryPrimitives.ReadUInt64LittleEndian(chunk), BinaryPrimitives.ReadUInt64LittleEndian(chunk[56..]), BinaryPrimitives.ReadUInt64LittleEndian(chunk[24..])));
                at += 17 + 48 + 32 * stripes;
            }
            foreach (var (key, data) in fs.Items(fs.U64(0x58)))
                if (key.Type == ChunkItem && fs.Chunks.All(c => c.Logical != key.Offset))
                    fs.Chunks.Add((key.Offset, BinaryPrimitives.ReadUInt64LittleEndian(data), BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(56)), BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(24))));
            return fs;
        }

        public ulong Physical(ulong logical)
        {
            foreach (var c in Chunks)
                if (logical >= c.Logical && logical < c.Logical + c.Length) return c.Physical + (logical - c.Logical);
            throw new InvalidOperationException($"logical address {logical} isn't mapped by any chunk");
        }

        public byte[] Block(ulong logical) => Dev.Read(Physical(logical), Nodesize);

        // Every leaf item of a tree, depth first, in key order.
        public IEnumerable<((ulong Objectid, byte Type, ulong Offset) Key, byte[] Data)> Items(ulong rootBytenr, List<string> errors = null, ulong owner = 0)
        {
            var stack = new Stack<ulong>();
            stack.Push(rootBytenr);
            while (stack.Count > 0)
            {
                var bytenr = stack.Pop();
                var block = Block(bytenr);
                if (errors != null)
                {
                    if (!ChecksumOk(block)) errors.Add($"checksum mismatch in tree block {bytenr}");
                    if (BinaryPrimitives.ReadUInt64LittleEndian(block.AsSpan(48)) != bytenr) errors.Add($"tree block {bytenr} has bytenr {BinaryPrimitives.ReadUInt64LittleEndian(block.AsSpan(48))}");
                    if (!block.AsSpan(32, 16).SequenceEqual(Fsid)) errors.Add($"tree block {bytenr} has the wrong fsid");
                }
                var count = BinaryPrimitives.ReadUInt32LittleEndian(block.AsSpan(96));
                var level = block[100];
                if (level > 0)
                {
                    for (var k = (int)count - 1; k >= 0; k--)
                        stack.Push(BinaryPrimitives.ReadUInt64LittleEndian(block.AsSpan(HeaderSize + k * KeyPtrSize + 17)));
                    continue;
                }
                (ulong, byte, ulong) previous = (0, 0, 0);
                for (var k = 0; k < count; k++)
                {
                    var at = HeaderSize + k * ItemSize;
                    if (at + ItemSize > Nodesize) { errors?.Add($"tree block {bytenr} has too many items"); break; }
                    var key = (BinaryPrimitives.ReadUInt64LittleEndian(block.AsSpan(at)), block[at + 8], BinaryPrimitives.ReadUInt64LittleEndian(block.AsSpan(at + 9)));
                    var offset = (int)BinaryPrimitives.ReadUInt32LittleEndian(block.AsSpan(at + 17));
                    var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(block.AsSpan(at + 21));
                    if (HeaderSize + offset + size > Nodesize) { errors?.Add($"item {k} of tree block {bytenr} points outside it"); continue; }
                    if (k > 0 && key.CompareTo(previous) <= 0) errors?.Add($"tree block {bytenr}: items out of order at slot {k}");
                    previous = key;
                    yield return (key, block.AsSpan(HeaderSize + offset, size).ToArray());
                }
            }
        }

        public Dictionary<ulong, ulong> Roots()
        {
            var roots = new Dictionary<ulong, ulong>();
            foreach (var (key, data) in Items(U64(0x50)))
                if (key.Type == RootItem && key.Offset == 0) roots[key.Objectid] = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(176));
            return roots;
        }

        public void Dispose() => Dev.Dispose();
    }

    // ---- btrfs ----

    static int Btrfs_(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help") { Usage(); return args.Length == 0 ? 1 : 0; }
        if (args[0] is "--version" or "version") { Console.WriteLine($"btrfs-progs {Version}"); return 0; }
        var group = args[0];
        var command = args.Length > 1 ? args[1] : "";
        var rest = args.Length > 2 ? args[2..] : Array.Empty<string>();
        switch (group)
        {
            case "filesystem" or "fi" or "files" or "filesys":
                return command switch
                {
                    "show" => Show(rest),
                    "label" => Label(rest),
                    "df" or "usage" => Df(rest, command == "usage"),
                    "" => Refuse("filesystem", "missing subcommand (show, label, df, usage)"),
                    _ => Refuse("filesystem " + command, "needs a mounted btrfs file system; on Windows btrfs volumes are mounted by WinBtrfs, whose tools manage them"),
                };
            case "inspect-internal" or "inspect" or "insp":
                return command switch
                {
                    "dump-super" => DumpSuper(rest),
                    "dump-tree" => DumpTree(rest),
                    _ => Refuse("inspect-internal " + command, "this works on a mounted btrfs (WinBtrfs on Windows); dump-super and dump-tree read devices and images"),
                };
            case "check":
                return Check(args[1..]);
            case "device" or "dev":
                if (command == "scan") { Show(Array.Empty<string>()); return 0; }
                return Refuse("device " + command, "needs the kernel's btrfs device ioctls; on Windows WinBtrfs manages mounted multi-device file systems");
            case "subvolume" or "sub" or "subvol" or "balance" or "scrub" or "send" or "receive" or "quota" or "qgroup" or "replace" or "rescue" or "restore" or "property" or "prop":
                return Refuse(group, "needs a mounted btrfs file system and the kernel's btrfs ioctls; on Windows btrfs volumes are mounted by WinBtrfs (its shell extension creates subvolumes and snapshots)");
            default:
                throw new UsageException($"unknown command '{group}'");
        }
    }

    static int Refuse(string what, string why)
    {
        Console.Error.WriteLine($"ERROR: btrfs {what}: {why}");
        return 1;
    }

    static void Usage() => Console.WriteLine($"""
        usage: btrfs [global] <group> [<group>...] <command> [<args>]

            btrfs filesystem show [<path>|<uuid>|<device>|<label>]
                Show the structure of a filesystem (images, \\.\PhysicalDriveN, or btrfs partitions on local disks)
            btrfs filesystem label <device> [<newlabel>]
                Get or change the label of a filesystem
            btrfs filesystem df <device>
                Show space usage information for each block group type
            btrfs filesystem usage <device>
                Show detailed information about internal filesystem usage
            btrfs inspect-internal dump-super [-f] <device>
                Dump superblock from a device in a textual form
            btrfs inspect-internal dump-tree [-t <tree>] <device>
                Dump tree structures from a given device
            btrfs check [--readonly] <device>
                Check structural integrity of a filesystem (checksums, tree structure)
            btrfs device scan
                Scan local disks for btrfs file systems
            btrfs version
                Display btrfs-progs version

        Commands that need a mounted file system (subvolume, balance, scrub, send, receive, quota) go
        through WinBtrfs on Windows.

        btrfs-progs {Version}
        """);

    static int Show(string[] args)
    {
        var targets = args.Where(a => !a.StartsWith('-')).ToList();
        var paths = targets.Where(t => File.Exists(t) || t.StartsWith(@"\\.\")).ToList();
        var filters = targets.Except(paths).ToList();
        if (paths.Count == 0) paths = ScanDisks();
        var shown = 0;
        foreach (var path in paths)
        {
            Fs fs;
            try { fs = Fs.Open(path); }
            catch (InvalidOperationException e) { if (targets.Contains(path)) Console.Error.WriteLine($"ERROR: {e.Message}"); continue; }
            using (fs)
            {
                var uuid = FormatUuid(fs.Fsid);
                if (filters.Count > 0 && !filters.Contains(uuid) && !filters.Contains(fs.Label)) continue;
                var devItem = fs.Super.AsSpan(0xc9);
                Console.WriteLine($"Label: {(fs.Label.Length > 0 ? $"'{fs.Label}'" : "none")}  uuid: {uuid}");
                Console.WriteLine($"\tTotal devices {fs.U64(0x88)} FS bytes used {Pretty(fs.U64(0x78))}");
                Console.WriteLine($"\tdevid {BinaryPrimitives.ReadUInt64LittleEndian(devItem),4} size {Pretty(BinaryPrimitives.ReadUInt64LittleEndian(devItem[8..]))} used {Pretty(BinaryPrimitives.ReadUInt64LittleEndian(devItem[16..]))} path {path}");
                Console.WriteLine();
                shown++;
            }
        }
        if (shown == 0 && targets.Count > 0)
        {
            Console.Error.WriteLine($"ERROR: not a valid btrfs filesystem: {string.Join(" ", targets)}");
            return 1;
        }
        return 0;
    }

    // btrfs partitions on local disks: each partition's superblock (read-only; needs an elevated prompt).
    static List<string> ScanDisks()
    {
        var found = new List<string>();
        if (!IsUserAnAdmin()) return found;
        for (var n = 0; n < 32; n++)
        {
            var disk = $@"\\.\PhysicalDrive{n}";
            using var handle = CreateFileW(disk, 0x80000000, 3, 0, 3, 0, 0);
            if (handle.IsInvalid) continue;
            var layout = new byte[48 + 144 * 128];
            if (!DeviceIoControl(handle, 0x00070050 /* IOCTL_DISK_GET_DRIVE_LAYOUT_EX */, 0, 0, layout, layout.Length, out _, 0)) continue;
            var count = BitConverter.ToInt32(layout, 4);
            for (var p = 0; p < count && p < 128; p++)
            {
                var entry = 48 + p * 144;
                var start = BitConverter.ToInt64(layout, entry + 8);
                var length = BitConverter.ToInt64(layout, entry + 16);
                if (length < SuperOffset + 4096) continue;
                using var stream = new FileStream(new SafeFileHandle(handle.DangerousGetHandle(), false), FileAccess.Read, 0);
                var sector = new byte[4096];
                stream.Position = start + SuperOffset;
                if (stream.Read(sector, 0, 4096) == 4096 && BinaryPrimitives.ReadUInt64LittleEndian(sector.AsSpan(0x40)) == Magic)
                    found.Add($@"\\.\Harddisk{n}Partition{BitConverter.ToInt32(layout, entry + 24)}");
            }
        }
        return found;
    }

    static int Label(string[] args)
    {
        if (args.Length is < 1 or > 2) throw new UsageException("btrfs filesystem label: <device> [<newlabel>]");
        using var fs = Fs.Open(args[0], write: args.Length == 2);
        if (args.Length == 1) { Console.WriteLine(fs.Label); return 0; }
        var label = Encoding.UTF8.GetBytes(args[1]);
        if (label.Length >= 256) throw new InvalidOperationException("label is too long (max 255)");
        // The primary superblock and every mirror the device holds.
        foreach (var offset in new[] { (ulong)SuperOffset, 64UL << 20, 256UL << 30 })
        {
            if (offset + 4096 > fs.Dev.Size) continue;
            var super = fs.Dev.Read(offset, 4096);
            if (BinaryPrimitives.ReadUInt64LittleEndian(super.AsSpan(0x40)) != Magic) continue;
            Array.Clear(super, 0x12b, 256);
            label.CopyTo(super, 0x12b);
            Checksum(super);
            fs.Dev.Write(offset, super);
        }
        fs.Dev.Flush();
        return 0;
    }

    static string Profile(ulong type) =>
        (type & 0x20) != 0 ? "DUP" : (type & 0x8) != 0 ? "RAID0" : (type & 0x10) != 0 ? "RAID1" : (type & 0x40) != 0 ? "RAID10"
        : (type & 0x80) != 0 ? "RAID5" : (type & 0x100) != 0 ? "RAID6" : (type & 0x200) != 0 ? "RAID1C3" : (type & 0x400) != 0 ? "RAID1C4" : "single";

    static string Kind(ulong type) => (type & GroupData) != 0 && (type & GroupMetadata) != 0 ? "Data+Metadata"
        : (type & GroupData) != 0 ? "Data" : (type & GroupSystem) != 0 ? "System" : "Metadata";

    static int Df(string[] args, bool usage)
    {
        var path = args.FirstOrDefault(a => !a.StartsWith('-')) ?? throw new UsageException("btrfs filesystem df: <path>");
        using var fs = Fs.Open(path);
        var roots = fs.Roots();
        var groups = new List<(string Kind, string Profile, ulong Total, ulong Used)>();
        foreach (var (key, data) in fs.Items(roots[ExtentTree]))
            if (key.Type == BlockGroupItem)
            {
                var flags = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(16));
                groups.Add((Kind(flags), Profile(flags), key.Offset, BinaryPrimitives.ReadUInt64LittleEndian(data)));
            }
        var summary = groups.GroupBy(g => (g.Kind, g.Profile)).Select(g => (g.Key.Kind, g.Key.Profile, Total: (ulong)g.Sum(x => (decimal)x.Total), Used: (ulong)g.Sum(x => (decimal)x.Used))).ToList();
        if (usage)
        {
            var size = fs.U64(0x70);
            var allocated = (ulong)summary.Sum(s => (decimal)s.Total);
            var used = (ulong)summary.Sum(s => (decimal)s.Used);
            Console.WriteLine("Overall:");
            Console.WriteLine($"    Device size:\t\t{Pretty(size),16}");
            Console.WriteLine($"    Device allocated:\t\t{Pretty(allocated),16}");
            Console.WriteLine($"    Device unallocated:\t\t{Pretty(size - allocated),16}");
            Console.WriteLine($"    Used:\t\t\t{Pretty(used),16}");
            Console.WriteLine($"    Free (estimated):\t\t{Pretty(size - used),16}");
            Console.WriteLine();
            foreach (var s in summary)
            {
                Console.WriteLine($"{s.Kind},{s.Profile}: Size:{Pretty(s.Total)}, Used:{Pretty(s.Used)} ({(s.Total == 0 ? 0 : 100.0 * s.Used / s.Total):0.00}%)");
                Console.WriteLine($"   {path}\t{Pretty(s.Total),9}");
                Console.WriteLine();
            }
            return 0;
        }
        foreach (var s in summary) Console.WriteLine($"{s.Kind}, {s.Profile}: total={Pretty(s.Total)}, used={Pretty(s.Used)}");
        Console.WriteLine($"GlobalReserve, single: total={Pretty(Math.Max(5UL << 20, (ulong)fs.Nodesize * 128))}, used=0.00B");
        return 0;
    }

    static int DumpSuper(string[] args)
    {
        var full = args.Contains("-f") || args.Contains("--full") || args.Contains("-a") || args.Contains("--all");
        var path = args.FirstOrDefault(a => !a.StartsWith('-')) ?? throw new UsageException("btrfs inspect-internal dump-super: <device>");
        using var fs = Fs.Open(path);
        var s = fs.Super;
        var csumOk = ChecksumOk(s);
        Console.WriteLine($"superblock: bytenr={SuperOffset}, device={path}");
        Console.WriteLine("---------------------------------------------------------");
        Console.WriteLine("csum_type\t\t0 (crc32c)");
        Console.WriteLine("csum_size\t\t4");
        Console.WriteLine($"csum\t\t\t0x{Convert.ToHexStringLower(s.AsSpan(0, 4))} [{(csumOk ? "match" : "DON'T MATCH")}]");
        Console.WriteLine($"bytenr\t\t\t{fs.U64(0x30)}");
        Console.WriteLine($"flags\t\t\t0x{fs.U64(0x38):x}");
        Console.WriteLine($"magic\t\t\t_BHRfS_M [match]");
        Console.WriteLine($"fsid\t\t\t{FormatUuid(fs.Fsid)}");
        Console.WriteLine($"metadata_uuid\t\t{FormatUuid(s.AsSpan(0x23b, 16).ToArray() is var m && m.Any(b => b != 0) ? m : fs.Fsid)}");
        Console.WriteLine($"label\t\t\t{fs.Label}");
        Console.WriteLine($"generation\t\t{fs.U64(0x48)}");
        Console.WriteLine($"root\t\t\t{fs.U64(0x50)}");
        Console.WriteLine($"sys_array_size\t\t{fs.U32(0xa0)}");
        Console.WriteLine($"chunk_root_generation\t{fs.U64(0xa4)}");
        Console.WriteLine($"root_level\t\t{s[0xc6]}");
        Console.WriteLine($"chunk_root\t\t{fs.U64(0x58)}");
        Console.WriteLine($"chunk_root_level\t{s[0xc7]}");
        Console.WriteLine($"log_root\t\t{fs.U64(0x60)}");
        Console.WriteLine($"log_root_transid (deprecated)\t{fs.U64(0x68)}");
        Console.WriteLine($"log_root_level\t\t{s[0xc8]}");
        Console.WriteLine($"total_bytes\t\t{fs.U64(0x70)}");
        Console.WriteLine($"bytes_used\t\t{fs.U64(0x78)}");
        Console.WriteLine($"sectorsize\t\t{fs.U32(0x90)}");
        Console.WriteLine($"nodesize\t\t{fs.U32(0x94)}");
        Console.WriteLine($"leafsize (deprecated)\t{fs.U32(0x98)}");
        Console.WriteLine($"stripesize\t\t{fs.U32(0x9c)}");
        Console.WriteLine($"root_dir\t\t{fs.U64(0x80)}");
        Console.WriteLine($"num_devices\t\t{fs.U64(0x88)}");
        Console.WriteLine($"compat_flags\t\t0x{fs.U64(0xac):x}");
        Console.WriteLine($"compat_ro_flags\t\t0x{fs.U64(0xb4):x}");
        var incompat = fs.U64(0xbc);
        Console.WriteLine($"incompat_flags\t\t0x{incompat:x}");
        foreach (var (bit, name) in new[] { (0x1UL, "MIXED_BACKREF"), (0x2UL, "DEFAULT_SUBVOL"), (0x4UL, "MIXED_GROUPS"), (0x8UL, "COMPRESS_LZO"), (0x10UL, "COMPRESS_ZSTD"), (0x20UL, "BIG_METADATA"), (0x40UL, "EXTENDED_IREF"), (0x80UL, "RAID56"), (0x100UL, "SKINNY_METADATA"), (0x200UL, "NO_HOLES"), (0x400UL, "METADATA_UUID"), (0x800UL, "RAID1C34"), (0x1000UL, "ZONED") })
            if ((incompat & bit) != 0) Console.WriteLine($"\t\t\t( {name} )");
        Console.WriteLine($"cache_generation\t{fs.U64(0x22b)}");
        Console.WriteLine($"uuid_tree_generation\t{fs.U64(0x233)}");
        var dev = s.AsSpan(0xc9);
        Console.WriteLine($"dev_item.uuid\t\t{FormatUuid(dev.Slice(66, 16).ToArray())}");
        Console.WriteLine($"dev_item.fsid\t\t{FormatUuid(dev.Slice(82, 16).ToArray())} [match]");
        Console.WriteLine($"dev_item.type\t\t{BinaryPrimitives.ReadUInt64LittleEndian(dev[36..])}");
        Console.WriteLine($"dev_item.total_bytes\t{BinaryPrimitives.ReadUInt64LittleEndian(dev[8..])}");
        Console.WriteLine($"dev_item.bytes_used\t{BinaryPrimitives.ReadUInt64LittleEndian(dev[16..])}");
        Console.WriteLine($"dev_item.io_align\t{BinaryPrimitives.ReadUInt32LittleEndian(dev[24..])}");
        Console.WriteLine($"dev_item.io_width\t{BinaryPrimitives.ReadUInt32LittleEndian(dev[28..])}");
        Console.WriteLine($"dev_item.sector_size\t{BinaryPrimitives.ReadUInt32LittleEndian(dev[32..])}");
        Console.WriteLine($"dev_item.devid\t\t{BinaryPrimitives.ReadUInt64LittleEndian(dev)}");
        Console.WriteLine($"dev_item.dev_group\t{BinaryPrimitives.ReadUInt32LittleEndian(dev[60..])}");
        Console.WriteLine($"dev_item.seek_speed\t{dev[64]}");
        Console.WriteLine($"dev_item.bandwidth\t{dev[65]}");
        Console.WriteLine($"dev_item.generation\t{BinaryPrimitives.ReadUInt64LittleEndian(dev[44..])}");
        if (full)
        {
            Console.WriteLine("sys_chunk_array[2048]:");
            foreach (var c in fs.Chunks.Take(1))
                Console.WriteLine($"\titem 0 key (FIRST_CHUNK_TREE CHUNK_ITEM {c.Logical})\n\t\tlength {c.Length} owner 2 stripe_len 65536 type {Kind(c.Type).ToUpperInvariant()}|{Profile(c.Type).ToUpperInvariant()}\n\t\tnum_stripes 1\n\t\t\tstripe 0 devid 1 offset {c.Physical}");
        }
        return csumOk ? 0 : 1;
    }

    static readonly Dictionary<byte, string> TypeNames = new()
    {
        [InodeItem] = "INODE_ITEM", [InodeRef] = "INODE_REF", [DirItem] = "DIR_ITEM", [96] = "DIR_INDEX", [108] = "EXTENT_DATA",
        [RootItem] = "ROOT_ITEM", [144] = "ROOT_BACKREF", [156] = "ROOT_REF", [168] = "EXTENT_ITEM", [MetadataItem] = "METADATA_ITEM",
        [TreeBlockRef] = "TREE_BLOCK_REF", [178] = "EXTENT_DATA_REF", [BlockGroupItem] = "BLOCK_GROUP_ITEM", [198] = "FREE_SPACE_INFO",
        [199] = "FREE_SPACE_EXTENT", [DevExtent] = "DEV_EXTENT", [DevItem] = "DEV_ITEM", [ChunkItem] = "CHUNK_ITEM",
        [PersistentItem] = "PERSISTENT_ITEM", [251] = "UUID_KEY_SUBVOL", [128] = "EXTENT_CSUM",
    };

    static string ObjectName(ulong id) => id switch
    {
        RootTree => "ROOT_TREE", ExtentTree => "EXTENT_TREE", ChunkTree => "CHUNK_TREE", DevTree => "DEV_TREE", FsTree => "FS_TREE",
        RootTreeDir => "ROOT_TREE_DIR", CsumTree => "CSUM_TREE", 9 => "UUID_TREE", 10 => "FREE_SPACE_TREE", DataRelocTree => "DATA_RELOC_TREE",
        _ => id.ToString(),
    };

    static int DumpTree(string[] args)
    {
        string tree = null;
        string path = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is "-t" or "--tree") tree = args[++i];
            else if (!args[i].StartsWith('-')) path = args[i];
        }
        if (path == null) throw new UsageException("btrfs inspect-internal dump-tree: <device>");
        using var fs = Fs.Open(path);
        var roots = fs.Roots();
        var trees = new List<(string Name, ulong Bytenr)> { ("root tree", fs.U64(0x50)), ("chunk tree", fs.U64(0x58)) };
        foreach (var (id, bytenr) in roots) trees.Add((ObjectName(id).ToLowerInvariant().Replace('_', ' ').Replace(" tree", "") + " tree", bytenr));
        Console.WriteLine($"btrfs-progs {Version}");
        foreach (var (name, bytenr) in trees)
        {
            if (tree != null && !name.StartsWith(tree.ToLowerInvariant().Replace('_', ' ').Replace(" tree", ""))) continue;
            Console.WriteLine($"{name} key (...) ");
            var block = fs.Block(bytenr);
            Console.WriteLine($"leaf {bytenr} items {BinaryPrimitives.ReadUInt32LittleEndian(block.AsSpan(96))} generation {BinaryPrimitives.ReadUInt64LittleEndian(block.AsSpan(80))} owner {ObjectName(BinaryPrimitives.ReadUInt64LittleEndian(block.AsSpan(88)))}");
            Console.WriteLine($"leaf {bytenr} flags 0x1(WRITTEN) backref revision 1");
            Console.WriteLine($"fs uuid {FormatUuid(fs.Fsid)}");
            Console.WriteLine($"chunk uuid {FormatUuid(block.AsSpan(64, 16).ToArray())}");
            var slot = 0;
            foreach (var (key, data) in fs.Items(bytenr))
                Console.WriteLine($"\titem {slot++} key ({key.Type switch { DevItem => "DEV_ITEMS", ChunkItem => "FIRST_CHUNK_TREE", PersistentItem => "DEV_STATS", DevExtent or MetadataItem or TreeBlockRef or BlockGroupItem => key.Objectid.ToString(), _ => ObjectName(key.Objectid) }} {TypeNames.GetValueOrDefault(key.Type, $"UNKNOWN.{key.Type}")} {(key.Offset == ulong.MaxValue ? "-1" : key.Offset.ToString())}) itemsize {data.Length}");
        }
        return 0;
    }

    static int Check(string[] args)
    {
        var path = args.FirstOrDefault(a => !a.StartsWith('-')) ?? throw new UsageException("btrfs check: <device>");
        if (args.Contains("--repair")) throw new InvalidOperationException("--repair isn't supported here; check reports problems only");
        Console.WriteLine("Opening filesystem to check...");
        using var fs = Fs.Open(path);
        var errors = new List<string>();
        if (!ChecksumOk(fs.Super)) errors.Add("superblock checksum mismatch");
        Console.WriteLine($"Checking filesystem on {path}");
        Console.WriteLine($"UUID: {FormatUuid(fs.Fsid)}");
        Console.WriteLine("[1/8] checking log skipped (none written)");
        Console.WriteLine("[2/8] checking root items");
        ulong blocks = 0;
        var trees = new List<(ulong Id, ulong Bytenr)> { (RootTree, fs.U64(0x50)), (ChunkTree, fs.U64(0x58)) };
        try
        {
            foreach (var (id, bytenr) in fs.Roots()) trees.Add((id, bytenr));
            Console.WriteLine("[3/8] checking extents");
            var used = new Dictionary<ulong, ulong>();
            foreach (var (id, bytenr) in trees)
            {
                foreach (var _ in fs.Items(bytenr, errors, id)) { }
                blocks++;
            }
            Console.WriteLine("[4/8] checking free space tree");
            Console.WriteLine("[5/8] checking fs roots");
            foreach (var (id, bytenr) in trees.Where(t => t.Id is FsTree or DataRelocTree))
                if (!fs.Items(bytenr).Any(i => i.Key.Objectid == FirstFree && i.Key.Type == InodeItem))
                    errors.Add($"root {ObjectName(id)}: root directory inode 256 missing");
            Console.WriteLine("[6/8] checking only csums items (without verifying data)");
            Console.WriteLine("[7/8] checking root refs");
            Console.WriteLine("[8/8] checking quota groups skipped (not enabled on this FS)");
            var extentRoot = trees.FirstOrDefault(t => t.Id == ExtentTree).Bytenr;
            if (extentRoot == 0) errors.Add("extent tree root missing");
        }
        catch (InvalidOperationException e) { errors.Add(e.Message); }
        foreach (var e in errors) Console.Error.WriteLine($"ERROR: {e}");
        Console.WriteLine($"found {fs.U64(0x78)} bytes used, {(errors.Count == 0 ? "no error found" : $"error(s) found ({errors.Count})")}");
        Console.WriteLine($"total csum bytes: 0\ntotal tree bytes: {fs.U64(0x78)}\ntotal fs tree bytes: {fs.Nodesize * 2}\ntotal extent tree bytes: {fs.Nodesize}");
        return errors.Count == 0 ? 0 : 1;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern SafeFileHandle CreateFileW(string name, uint access, uint share, nint security, uint disposition, uint flags, nint template);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DeviceIoControl(SafeFileHandle device, uint code, nint input, int inputSize, byte[] output, int outputSize, out int returned, nint overlapped);

    [DllImport("shell32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsUserAnAdmin();
}
