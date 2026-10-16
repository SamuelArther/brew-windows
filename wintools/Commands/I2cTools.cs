using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace WinTools;

// i2c-tools on Windows: Linux's /dev/i2c-N buses are the I2C controllers Windows exposes to user
// mode (Windows.Devices.I2c, the SPB framework's controllers that the firmware makes available, as
// on many tablets, embedded boards and Windows IoT). Bus N is the Nth controller in device
// enumeration order (or give its name). i2cdetect scans with a one-byte read (or a zero-length
// "quick" write with -q), i2cget/i2cset/i2cdump use register reads/writes (write the register
// address, then read, with a repeated start), i2ctransfer runs combined transfers, eeprog reads and
// writes 24Cxx EEPROMs. Windows doesn't expose the SMBus host of PCs or SPD EEPROMs to user mode,
// so decode-dimms decodes the memory modules from the SMBIOS memory device records instead.
static class I2cTools
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("i2cdetect", "Detect I2C chips (list buses with -l)", Detect);
        Commands.Add("i2cget", "Read from I2C chip registers", Get);
        Commands.Add("i2cset", "Set I2C chip registers", Set);
        Commands.Add("i2cdump", "Examine I2C chip registers", Dump);
        Commands.Add("i2ctransfer", "Send user-defined I2C messages in one transfer", Transfer);
        Commands.Add("eeprog", "Read and write 24Cxx EEPROMs over I2C", Eeprog);
        Commands.Add("decode-dimms", "Decode the memory modules (SMBIOS memory device records)", DecodeDimms);
    }

    const string Version = "4.4";

    static T Await<T>(Windows.Foundation.IAsyncOperation<T> op) => op.AsTask().GetAwaiter().GetResult();

    // ---- buses ----

    sealed class Bus
    {
        public int Number;
        public string Id, Name;
    }

    static List<Bus> Buses()
    {
        var list = new List<Bus>();
        DeviceInformationCollection found;
        try { found = Await(DeviceInformation.FindAllAsync(I2cDevice.GetDeviceSelector())); }
        catch (COMException) { return list; }
        foreach (var d in found)
            list.Add(new Bus { Number = list.Count, Id = d.Id, Name = string.IsNullOrEmpty(d.Name) ? d.Id : d.Name });
        return list;
    }

    static Bus FindBus(string spec)
    {
        var buses = Buses();
        var s = spec.StartsWith("i2c-") ? spec[4..] : spec;
        var bus = int.TryParse(s, out var n) ? buses.FirstOrDefault(b => b.Number == n)
            : buses.FirstOrDefault(b => b.Name.Equals(spec, StringComparison.OrdinalIgnoreCase) || b.Id.Equals(spec, StringComparison.OrdinalIgnoreCase));
        if (bus != null) return bus;
        if (!int.TryParse(s, out n)) throw new UsageException($"Error: I2C bus name doesn't match any bus present!");
        Console.Error.WriteLine($"Error: Could not open file `/dev/i2c-{n}' or `/dev/i2c/{n}': No such file or directory");
        Console.Error.WriteLine("(Windows lists only the I2C controllers its firmware exposes to applications; see i2cdetect -l)");
        throw new BusMissing();
    }

    sealed class BusMissing : Exception;

    static int Guard(Func<int> body)
    {
        try { return body(); }
        catch (BusMissing) { return 1; }
        catch (COMException e) { Console.Error.WriteLine($"Error: {e.Message}"); return 1; }
    }

    static I2cDevice Open(Bus bus, int address, bool force)
    {
        var settings = new I2cConnectionSettings(address) { BusSpeed = I2cBusSpeed.StandardMode, SharingMode = I2cSharingMode.Shared };
        I2cDevice device;
        try { device = Await(I2cDevice.FromIdAsync(bus.Id, settings)); }
        catch (Exception e) when (e is COMException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"Could not set address to 0x{address:x2}: {e.Message}");
        }
        if (device == null) throw new InvalidOperationException($"Could not set address to 0x{address:x2}: Device or resource busy{(force ? "" : " (try -f)")}");
        return device;
    }

    static int ParseAddress(string s, bool all = false)
    {
        var value = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? Convert.ToInt32(s[2..], 16) : int.Parse(s);
        if (value < (all ? 0x00 : 0x08) || value > (all ? 0x7f : 0x77)) throw new UsageException($"Error: Chip address out of range ({(all ? "0x00-0x7f" : "0x08-0x77")})!");
        return value;
    }

    static int ParseByte(string s, string what, int max = 0xff)
    {
        int value;
        try { value = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? Convert.ToInt32(s[2..], 16) : int.Parse(s); }
        catch (FormatException) { throw new UsageException($"Error: {what} invalid!"); }
        if (value < 0 || value > max) throw new UsageException($"Error: {what} out of range!");
        return value;
    }

    static bool Confirm(string message, bool yes)
    {
        if (yes) return true;
        Console.Error.Write(message + "Continue? [Y/n] ");
        var answer = Console.ReadLine();
        return answer == null || answer.Length == 0 || answer.StartsWith('y') || answer.StartsWith('Y');
    }

    // ---- i2cdetect ----

    static int Detect(string[] args) => Guard(() =>
    {
        bool yes = false, all = false, list = false, functionality = false;
        var mode = 'a';
        var positional = new List<string>();
        foreach (var a in args)
        {
            if (a == "-V" || a == "--version") { Console.WriteLine($"i2cdetect version {Version}"); return 0; }
            if (a is "-h" or "--help")
            {
                Console.WriteLine("Usage: i2cdetect [-y] [-a] [-q|-r] I2CBUS [FIRST LAST]\n       i2cdetect -F I2CBUS\n       i2cdetect -l\n  I2CBUS is an integer or an I2C bus name\n  If provided, FIRST and LAST limit the probing range.");
                return 0;
            }
            if (a.StartsWith('-') && a.Length > 1)
            {
                foreach (var c in a[1..])
                    switch (c)
                    {
                        case 'y': yes = true; break;
                        case 'a': all = true; break;
                        case 'l': list = true; break;
                        case 'F': functionality = true; break;
                        case 'q': mode = 'q'; break;
                        case 'r': mode = 'r'; break;
                        default: throw new UsageException($"Error: Unsupported option \"-{c}\"!");
                    }
                continue;
            }
            positional.Add(a);
        }
        if (list)
        {
            foreach (var b in Buses())
                Console.WriteLine($"i2c-{b.Number}\ti2c       \t{Truncate(b.Name, 32),-32}\tI2C adapter");
            return 0;
        }
        if (positional.Count == 0) throw new UsageException("Error: No i2c-bus specified!");
        var bus = FindBus(positional[0]);
        if (functionality)
        {
            Console.WriteLine($"Functionalities implemented by /dev/i2c-{bus.Number}:");
            foreach (var (name, yesno) in new[]
            {
                ("I2C", true), ("SMBus Quick Command", true), ("SMBus Send Byte", true), ("SMBus Receive Byte", true),
                ("SMBus Write Byte", true), ("SMBus Read Byte", true), ("SMBus Write Word", true), ("SMBus Read Word", true),
                ("SMBus Process Call", true), ("SMBus Block Write", true), ("SMBus Block Read", false), ("SMBus Block Process Call", false),
                ("SMBus PEC", false), ("I2C Block Write", true), ("I2C Block Read", true),
            })
                Console.WriteLine($"{name,-32}{(yesno ? "yes" : "no")}");
            return 0;
        }
        int first = all ? 0x00 : 0x08, last = all ? 0x7f : 0x77;
        if (positional.Count >= 3) { first = ParseAddress(positional[1], true); last = ParseAddress(positional[2], true); }
        if (!Confirm($"WARNING! This program can confuse your I2C bus, cause data loss and worse!\nI will probe file /dev/i2c-{bus.Number}{(mode == 'q' ? " using quick write commands" : mode == 'r' ? " using receive byte commands" : "")}.\nI will probe address range 0x{first:x2}-0x{last:x2}.\n", yes))
        {
            Console.Error.WriteLine("Aborting on user request.");
            return 0;
        }
        Console.WriteLine("     0  1  2  3  4  5  6  7  8  9  a  b  c  d  e  f");
        for (var row = 0; row < 128; row += 16)
        {
            var sb = new StringBuilder($"{row:x2}:");
            for (var col = 0; col < 16; col++)
            {
                var address = row + col;
                if (address < first || address > last) { sb.Append("   "); continue; }
                sb.Append(' ').Append(Probe(bus, address, mode) ? $"{address:x2}" : "--");
            }
            Console.WriteLine(sb.ToString());
        }
        return 0;
    });

    static string Truncate(string s, int n) => s.Length > n ? s[..n] : s;

    static bool Probe(Bus bus, int address, char mode)
    {
        I2cDevice device;
        try { device = Open(bus, address, force: true); }
        catch (InvalidOperationException) { return false; }
        using (device)
        {
            // Like i2cdetect's auto mode: quick write for most addresses, a read where writes are risky.
            var quick = mode == 'q' || (mode == 'a' && !(address >= 0x30 && address <= 0x37) && !(address >= 0x50 && address <= 0x5f));
            var result = quick ? device.WritePartial(Array.Empty<byte>()) : device.ReadPartial(new byte[1]);
            return result.Status is I2cTransferStatus.FullTransfer or I2cTransferStatus.PartialTransfer;
        }
    }

    // ---- i2cget / i2cset / i2cdump ----

    static (bool yes, bool force, bool all, List<string> rest) CommonOptions(string[] args, string tool, string usage)
    {
        bool yes = false, force = false, all = false;
        var rest = new List<string>();
        foreach (var a in args)
        {
            if (a is "-V" or "--version") { Console.WriteLine($"{tool} version {Version}"); Environment.Exit(0); }
            if (a is "-h" or "--help") { Console.WriteLine(usage); Environment.Exit(0); }
            if (a.StartsWith('-') && a.Length > 1 && rest.Count == 0 && !char.IsAsciiDigit(a[1]))
            {
                foreach (var c in a[1..])
                    switch (c)
                    {
                        case 'y': yes = true; break;
                        case 'f': force = true; break;
                        case 'a': all = true; break;
                        case 'r' or 'm': break;   // handled by the caller
                        default: throw new UsageException($"Error: Unsupported option \"-{c}\"!");
                    }
                continue;
            }
            rest.Add(a);
        }
        return (yes, force, all, rest);
    }

    static byte[] ReadRegister(I2cDevice device, int? register, int length)
    {
        var buffer = new byte[length];
        var result = register is int r ? device.WriteReadPartial(new[] { (byte)r }, buffer) : device.ReadPartial(buffer);
        if (result.Status != I2cTransferStatus.FullTransfer)
            throw new InvalidOperationException(result.Status == I2cTransferStatus.SlaveAddressNotAcknowledged ? "Read failed (no acknowledgement)" : "Read failed");
        return buffer;
    }

    static int Get(string[] args) => Guard(() =>
    {
        const string usage = "Usage: i2cget [-f] [-y] [-a] I2CBUS CHIP-ADDRESS [DATA-ADDRESS [MODE [LENGTH]]]\n  I2CBUS is an integer or an I2C bus name\n  ADDRESS is an integer (0x08 - 0x77, or 0x00 - 0x7f if -a is given)\n  MODE is one of:\n    b (read byte data, default)\n    w (read word data)\n    c (write byte/read byte)\n    s (read SMBus block data)\n    i (read I2C block data)\n    Append p for SMBus PEC";
        var (yes, force, all, rest) = CommonOptions(args, "i2cget", usage);
        if (rest.Count < 2) { Console.Error.WriteLine(usage); return 1; }
        var bus = FindBus(rest[0]);
        var address = ParseAddress(rest[1], all);
        int? register = rest.Count > 2 ? ParseByte(rest[2], "Data address") : null;
        var mode = rest.Count > 3 ? rest[3].TrimEnd('p') : register == null ? "byte" : "b";
        var length = rest.Count > 4 ? ParseByte(rest[4], "Length", 32) : 32;
        if (!Confirm($"WARNING! This program can confuse your I2C bus, cause data loss and worse!\nI will read from device file /dev/i2c-{bus.Number}, chip address 0x{address:x2}, {(register is int r0 ? $"data address\n0x{r0:x2}" : "current data\naddress")}, using {mode}.\n", yes))
            return 0;
        using var device = Open(bus, address, force);
        switch (mode)
        {
            case "byte" or "b":
                Console.WriteLine($"0x{ReadRegister(device, register, 1)[0]:x2}");
                break;
            case "c":
                if (register is int c) device.Write(new[] { (byte)c });
                Console.WriteLine($"0x{ReadRegister(device, null, 1)[0]:x2}");
                break;
            case "w":
            {
                var data = ReadRegister(device, register, 2);
                Console.WriteLine($"0x{data[0] | data[1] << 8:x4}");
                break;
            }
            case "i" or "s":
            {
                var data = ReadRegister(device, register, mode == "s" ? 33 : length);
                var bytes = mode == "s" ? data.Skip(1).Take(Math.Min((int)data[0], 32)) : data;
                Console.WriteLine(string.Join(' ', bytes.Select(b => $"0x{b:x2}")));
                break;
            }
            default: throw new UsageException($"Error: Invalid mode '{mode}'!");
        }
        return 0;
    });

    static int Set(string[] args) => Guard(() =>
    {
        const string usage = "Usage: i2cset [-f] [-y] [-m MASK] [-r] [-a] I2CBUS CHIP-ADDRESS DATA-ADDRESS [VALUE] ... [MODE]\n  I2CBUS is an integer or an I2C bus name\n  ADDRESS is an integer (0x08 - 0x77, or 0x00 - 0x7f if -a is given)\n  MODE is one of:\n    c (byte, no value)\n    b (byte data, default)\n    w (word data)\n    i (I2C block data)\n    s (SMBus block data)\n    Append p for SMBus PEC";
        int? mask = null;
        var readback = args.Contains("-r");
        var filtered = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "-m") { mask = ParseByte(args[++i], "Data value mask", 0xffff); continue; }
            if (args[i] == "-r") continue;
            filtered.Add(args[i]);
        }
        var (yes, force, all, rest) = CommonOptions(filtered.ToArray(), "i2cset", usage);
        if (rest.Count < 3) { Console.Error.WriteLine(usage); return 1; }
        var bus = FindBus(rest[0]);
        var address = ParseAddress(rest[1], all);
        var register = ParseByte(rest[2], "Data address");
        var mode = "b";
        var values = rest.Skip(3).ToList();
        if (values.Count > 0 && values[^1].TrimEnd('p') is "b" or "w" or "c" or "i" or "s") { mode = values[^1].TrimEnd('p'); values.RemoveAt(values.Count - 1); }
        if (mode != "c" && values.Count == 0) throw new UsageException("Error: Value missing!");
        var data = new List<byte> { (byte)register };
        switch (mode)
        {
            case "c": break;
            case "b": data.Add((byte)ParseByte(values[0], "Data value")); break;
            case "w": { var v = ParseByte(values[0], "Data value", 0xffff); data.Add((byte)v); data.Add((byte)(v >> 8)); break; }
            case "i": data.AddRange(values.Select(v => (byte)ParseByte(v, "Data value"))); break;
            case "s": data.Add((byte)values.Count); data.AddRange(values.Select(v => (byte)ParseByte(v, "Data value"))); break;
        }
        if (!Confirm($"WARNING! This program can confuse your I2C bus, cause data loss and worse!\nI will write to device file /dev/i2c-{bus.Number}, chip address 0x{address:x2}, data address\n0x{register:x2}, using {mode}.\n", yes))
            return 0;
        using var device = Open(bus, address, force);
        if (mask is int m && mode is "b" or "w")
        {
            // -m: read-modify-write of the masked bits only.
            var old = ReadRegister(device, register, mode == "w" ? 2 : 1);
            var oldValue = mode == "w" ? old[0] | old[1] << 8 : old[0];
            var newValue = mode == "w" ? data[1] | data[2] << 8 : data[1];
            var merged = (newValue & m) | (oldValue & ~m);
            data = mode == "w" ? new List<byte> { (byte)register, (byte)merged, (byte)(merged >> 8) } : new List<byte> { (byte)register, (byte)merged };
        }
        var result = device.WritePartial(data.ToArray());
        if (result.Status != I2cTransferStatus.FullTransfer) throw new InvalidOperationException("Write failed");
        if (readback && mode is "b" or "w")
        {
            var back = ReadRegister(device, register, mode == "w" ? 2 : 1);
            var expected = mode == "w" ? data[1] | data[2] << 8 : data[1];
            var got = mode == "w" ? back[0] | back[1] << 8 : back[0];
            Console.WriteLine(got == expected ? $"Value 0x{got:x2} written, readback matched" : $"Warning - data mismatch - wrote 0x{expected:x2}, read back 0x{got:x2}");
        }
        return 0;
    });

    static int Dump(string[] args) => Guard(() =>
    {
        const string usage = "Usage: i2cdump [-f] [-y] [-r first-last] [-a] I2CBUS ADDRESS [MODE [BANK [BANKREG]]]\n  I2CBUS is an integer or an I2C bus name\n  ADDRESS is an integer (0x08 - 0x77, or 0x00 - 0x7f if -a is given)\n  MODE is one of:\n    b (byte, default)\n    w (word)\n    W (word on even register addresses)\n    i (I2C block)\n    c (consecutive byte)";
        int first = 0, last = 0xff;
        var filtered = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "-r")
            {
                var range = args[++i].Split('-');
                first = ParseByte(range[0], "First register");
                last = ParseByte(range[1], "Last register");
                continue;
            }
            filtered.Add(args[i]);
        }
        var (yes, force, all, rest) = CommonOptions(filtered.ToArray(), "i2cdump", usage);
        if (rest.Count < 2) { Console.Error.WriteLine(usage); return 1; }
        var bus = FindBus(rest[0]);
        var address = ParseAddress(rest[1], all);
        var mode = rest.Count > 2 ? rest[2] : "b";
        if (!Confirm($"WARNING! This program can confuse your I2C bus, cause data loss and worse!\nI will probe file /dev/i2c-{bus.Number}, address 0x{address:x2}, mode {mode switch { "w" or "W" => "word", "i" => "i2c block", "c" => "byte consecutive read", _ => "byte" }}\n", yes))
            return 0;
        using var device = Open(bus, address, force);
        var values = new int[256];
        Array.Fill(values, -1);
        if (mode is "i" or "c")
        {
            // One block read from the first register (consecutive reads auto-increment on most chips).
            var block = ReadRegister(device, mode == "i" ? first : null, last - first + 1);
            for (var k = 0; k < block.Length; k++) values[first + k] = block[k];
        }
        else
            for (var reg = first; reg <= last; reg++)
            {
                if (mode == "W" && reg % 2 == 1) continue;
                try
                {
                    var data = ReadRegister(device, reg, mode is "w" or "W" ? 2 : 1);
                    values[reg] = mode is "w" or "W" ? data[0] | data[1] << 8 : data[0];
                }
                catch (InvalidOperationException) { }
            }
        if (mode is "w" or "W")
        {
            Console.WriteLine("     0,8  1,9  2,a  3,b  4,c  5,d  6,e  7,f");
            for (var row = first & ~7; row <= last; row += 8)
            {
                var sb = new StringBuilder($"{row:x2}: ");
                for (var col = 0; col < 8; col++)
                {
                    var reg = row + col;
                    sb.Append(reg < first || reg > last ? "     " : values[reg] < 0 ? "XXXX " : $"{values[reg]:x4} ");
                }
                Console.WriteLine(sb.ToString().TrimEnd());
            }
            return 0;
        }
        Console.WriteLine("     0  1  2  3  4  5  6  7  8  9  a  b  c  d  e  f    0123456789abcdef");
        for (var row = first & ~0xf; row <= last; row += 16)
        {
            var hex = new StringBuilder($"{row:x2}: ");
            var text = new StringBuilder();
            for (var col = 0; col < 16; col++)
            {
                var reg = row + col;
                if (reg < first || reg > last) { hex.Append("   "); text.Append(' '); continue; }
                var v = values[reg];
                hex.Append(v < 0 ? "XX " : $"{v:x2} ");
                text.Append(v < 0 ? 'X' : v is 0x00 or 0xff ? '.' : v < 32 || v >= 127 ? '?' : (char)v);
            }
            Console.WriteLine($"{hex}   {text}");
        }
        return 0;
    });

    // ---- i2ctransfer ----

    static int Transfer(string[] args) => Guard(() =>
    {
        const string usage = "Usage: i2ctransfer [OPTIONS] I2CBUS DESC [DATA] [DESC [DATA]]...\n  OPTIONS: -a allow even reserved addresses\n           -f force access even if address is marked used\n           -h this help text\n           -v verbose mode\n           -V version info\n           -y yes to all confirmations\n  I2CBUS is an integer or an I2C bus name\n  DESC describes the transfer in the form: {r|w}LENGTH[@address]\n    1) read/write-flag 2) LENGTH (range 0-65535, or '?')\n    3) I2C address (use last one if omitted)\n  DATA are LENGTH bytes for a write message. They can be shortened by a suffix:\n    = (keep value constant until LENGTH)\n    + (increase value by 1 until LENGTH)\n    - (decrease value by 1 until LENGTH)\n    p (use pseudo random generator until LENGTH with value as seed)";
        var verbose = args.Contains("-v");
        var (yes, force, all, rest) = CommonOptions(args.Where(a => a != "-v").ToArray(), "i2ctransfer", usage);
        if (rest.Count < 2) { Console.Error.WriteLine(usage); return 1; }
        var bus = FindBus(rest[0]);
        var messages = new List<(bool Read, int Address, byte[] Data)>();
        var current = -1;
        for (var i = 1; i < rest.Count;)
        {
            var m = System.Text.RegularExpressions.Regex.Match(rest[i], @"^([rw])(\d+)(?:@(0x[0-9a-fA-F]+|\d+))?$");
            if (!m.Success) throw new UsageException($"Error: Invalid direction or length '{rest[i]}'");
            if (m.Groups[3].Success) current = ParseAddress(m.Groups[3].Value, all);
            if (current < 0) throw new UsageException("Error: No address given");
            var length = int.Parse(m.Groups[2].Value);
            i++;
            if (m.Groups[1].Value == "r") { messages.Add((true, current, new byte[length])); continue; }
            var data = new byte[length];
            var k = 0;
            while (k < length)
            {
                if (i >= rest.Count) throw new UsageException("Error: Missing data for write message");
                var token = rest[i++];
                var suffix = token[^1] is '=' or '+' or '-' or 'p' ? token[^1] : '\0';
                var value = ParseByte(suffix == '\0' ? token : token[..^1], "Data value");
                if (suffix == '\0') { data[k++] = (byte)value; continue; }
                var seed = value;
                while (k < length)
                {
                    data[k++] = (byte)value;
                    value = suffix switch { '+' => value + 1, '-' => value - 1, 'p' => (seed = (seed * 1103515245 + 12345) & 0x7fffffff) & 0xff, _ => value };
                }
            }
            messages.Add((false, current, data));
        }
        if (!Confirm($"WARNING! This program can confuse your I2C bus, cause data loss and worse!\nI will send the following messages to device file /dev/i2c-{bus.Number}:\n\n" +
                     string.Join('\n', messages.Select((m, n) => $"msg {n}: addr 0x{m.Address:x2}, {(m.Read ? "read" : "write")}, len {m.Data.Length}" +
                         (m.Read ? "" : ", buf " + string.Join(' ', m.Data.Select(b => $"0x{b:x2}"))))) + "\n", yes))
            return 0;
        // A write followed by a read of the same chip is one combined transfer (repeated start).
        for (var n = 0; n < messages.Count; n++)
        {
            var msg = messages[n];
            using var device = Open(bus, msg.Address, force);
            I2cTransferResult result;
            if (!msg.Read && n + 1 < messages.Count && messages[n + 1].Read && messages[n + 1].Address == msg.Address)
            {
                result = device.WriteReadPartial(msg.Data, messages[n + 1].Data);
                Print(n + 1, messages[n + 1].Address, messages[n + 1].Data, verbose);
                n++;
            }
            else if (msg.Read)
            {
                result = device.ReadPartial(msg.Data);
                Print(n, msg.Address, msg.Data, verbose);
            }
            else result = device.WritePartial(msg.Data);
            if (result.Status != I2cTransferStatus.FullTransfer)
            {
                Console.Error.WriteLine("Error: Sending messages failed: Input/output error");
                return 1;
            }
        }
        return 0;
    });

    static void Print(int n, int address, byte[] data, bool verbose) =>
        Console.WriteLine((verbose ? $"msg {n}: addr 0x{address:x2}, read, len {data.Length}, buf " : "") + string.Join(' ', data.Select(b => $"0x{b:x2}")));

    // ---- eeprog ----

    static int Eeprog(string[] args) => Guard(() =>
    {
        const string usage = "Usage: eeprog [-fqxdh] [-16|-8] [-r addr[:count]|-w addr] I2CBUS I2C-ADDRESS\n  Address modes:\n\t-8\t\tUse 8bit address mode for 24c0x...24C16 [default]\n\t-16\t\tUse 16bit address mode for 24c32...24C256\n  Actions:\n\t-r addr[:count]\tRead [count] (1 if omitted) bytes from [addr] and print them to the standard output\n\t-w addr\t\tWrite input (stdin) at address [addr] of the EEPROM\n\t-h\t\tPrint this help\n  Options:\n\t-x\t\tSet hex output mode\n\t-d\t\tDummy mode, display what *would* have been done\n\t-f\t\tDisable warnings and don't ask confirmation\n\t-q\t\tQuiet mode";
        bool sixteen = false, hex = false, dummy = false, force = false, quiet = false;
        int readAddress = -1, count = 1, writeAddress = -1;
        var rest = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h": Console.WriteLine(usage); return 0;
                case "-16": sixteen = true; break;
                case "-8": sixteen = false; break;
                case "-x": hex = true; break;
                case "-d": dummy = true; break;
                case "-f": force = true; break;
                case "-q": quiet = true; break;
                case "-r":
                {
                    var spec = args[++i].Split(':');
                    readAddress = ParseByte(spec[0], "address", 0xffff);
                    if (spec.Length > 1) count = ParseByte(spec[1], "count", 0x10000);
                    break;
                }
                case "-w": writeAddress = ParseByte(args[++i], "address", 0xffff); break;
                default: rest.Add(args[i]); break;
            }
        }
        if (rest.Count != 2 || (readAddress < 0) == (writeAddress < 0)) { Console.Error.WriteLine(usage); return 1; }
        var bus = FindBus(rest[0]);
        var address = ParseAddress(rest[1]);
        if (!quiet) Console.Error.WriteLine($"eeprog 0.7.6, a 24Cxx EEPROM reader/writer\n  Bus: i2c-{bus.Number}, Address: 0x{address:x2}, Mode: {(sixteen ? 16 : 8)}bit");
        if (dummy) { Console.Error.WriteLine(readAddress >= 0 ? $"  Would read {count} bytes from 0x{readAddress:x4}" : $"  Would write stdin at 0x{writeAddress:x4}"); return 0; }
        if (!force && writeAddress >= 0 && !Confirm("WARNING! The EEPROM will be overwritten.\n", false)) return 0;
        using var device = Open(bus, address, force);
        byte[] AddressBytes(int a) => sixteen ? new[] { (byte)(a >> 8), (byte)a } : new[] { (byte)a };
        if (readAddress >= 0)
        {
            var data = new byte[count];
            var result = device.WriteReadPartial(AddressBytes(readAddress), data);
            if (result.Status != I2cTransferStatus.FullTransfer) throw new InvalidOperationException("read error");
            if (hex)
            {
                for (var k = 0; k < data.Length; k += 16)
                    Console.WriteLine($"{readAddress + k:x4}|  " + string.Join(' ', data.Skip(k).Take(16).Select(b => $"{b:x2}")));
            }
            else
            {
                using var stdout = Console.OpenStandardOutput();
                stdout.Write(data);
            }
            return 0;
        }
        using var stdin = Console.OpenStandardInput();
        using var buffer = new MemoryStream();
        stdin.CopyTo(buffer);
        var input = buffer.ToArray();
        // Page-sized writes; the EEPROM needs a few milliseconds per page.
        const int page = 8;
        for (var k = 0; k < input.Length; k += page)
        {
            var chunk = AddressBytes(writeAddress + k).Concat(input.Skip(k).Take(page)).ToArray();
            if (device.WritePartial(chunk).Status != I2cTransferStatus.FullTransfer) throw new InvalidOperationException("write error");
            Thread.Sleep(10);
        }
        return 0;
    });

    // ---- decode-dimms (SMBIOS type 17) ----

    static int DecodeDimms(string[] args)
    {
        if (args.Contains("-h") || args.Contains("--help"))
        {
            Console.WriteLine("Usage: decode-dimms [-c] [-f [-b]] [-x|-X file [files..]]\n       decode-dimms -h\n\n  -h, --help             Display this usage summary\n\nOn Windows the modules are decoded from the SMBIOS memory device records.");
            return 0;
        }
        Console.WriteLine("# decode-dimms version 4.4\n\nMemory Serial Presence Detect Decoder\nBy Philip Edelbrock, Christian Zuckschwerdt, Burkart Lingner,\nJean Delvare, Trent Piepho and others\n");
        var devices = MemoryDevices();
        if (devices.Count == 0)
        {
            Console.WriteLine("No EEPROM found, the kernel probably does not support your hardware.");
            Console.WriteLine("(Windows doesn't expose SPD EEPROMs; the firmware's SMBIOS tables list no memory modules.)");
            return 0;
        }
        Console.WriteLine("SPD EEPROMs aren't readable on Windows without a driver; decoding the SMBIOS memory device records.\n");
        var n = 0;
        foreach (var d in devices)
        {
            n++;
            Console.WriteLine($"Decoding module {d["Locator"]}");
            Console.WriteLine($"Guessing DIMM is in                             {d["Bank"]} {d["Locator"]}".TrimEnd());
            Console.WriteLine("\n---=== SPD EEPROM Information ===---");
            Console.WriteLine($"{"Fundamental Memory type",-48}{d["Type"]}");
            Console.WriteLine($"{"Module Form Factor",-48}{d["FormFactor"]}");
            Console.WriteLine("\n---=== Memory Characteristics ===---");
            Console.WriteLine($"{"Maximum module speed",-48}{d["Speed"]}");
            Console.WriteLine($"{"Configured speed",-48}{d["Configured"]}");
            Console.WriteLine($"{"Size",-48}{d["Size"]}");
            Console.WriteLine($"{"Data Width / Total Width",-48}{d["Width"]}");
            Console.WriteLine($"{"Ranks",-48}{d["Rank"]}");
            Console.WriteLine($"{"Voltage",-48}{d["Voltage"]}");
            Console.WriteLine("\n---=== Manufacturer Data ===---");
            Console.WriteLine($"{"Module Manufacturer",-48}{d["Manufacturer"]}");
            Console.WriteLine($"{"Assembly Serial Number",-48}{d["Serial"]}");
            Console.WriteLine($"{"Part Number",-48}{d["Part"]}");
            Console.WriteLine();
        }
        Console.WriteLine($"\nNumber of SDRAM DIMMs detected and decoded: {n}");
        return 0;
    }

    static List<Dictionary<string, string>> MemoryDevices()
    {
        var result = new List<Dictionary<string, string>>();
        var size = GetSystemFirmwareTable(0x52534D42, 0, null, 0);   // 'RSMB'
        if (size == 0) return result;
        var raw = new byte[size];
        GetSystemFirmwareTable(0x52534D42, 0, raw, size);
        // RawSMBIOSData: 8-byte header, then the structure table.
        var at = 8;
        var end = Math.Min(raw.Length, 8 + (int)BitConverter.ToUInt32(raw, 4));
        while (at + 4 <= end)
        {
            var type = raw[at];
            var length = raw[at + 1];
            if (length < 4 || at + length > end) break;
            // The formatted part is followed by NUL-terminated strings and one more NUL (two when there are none).
            var strings = new List<string>();
            var s = at + length;
            int next;
            if (s < end && raw[s] == 0) next = s + 2;
            else
            {
                while (s < end && raw[s] != 0)
                {
                    var e = Array.IndexOf(raw, (byte)0, s);
                    if (e < 0) { e = end; }
                    strings.Add(Encoding.ASCII.GetString(raw, s, e - s).Trim());
                    s = e + 1;
                }
                next = s + 1;
            }
            if (type == 17 && length >= 0x15)
            {
                string Str(int offset) => offset < length && raw[at + offset] is var k and > 0 && k <= strings.Count ? strings[k - 1] : "Unknown";
                int Word(int offset) => offset + 1 < length ? BitConverter.ToUInt16(raw, at + offset) : 0;
                var sizeField = Word(0x0C);
                if (sizeField == 0) { at = next; continue; }   // empty slot
                long megabytes = sizeField == 0x7FFF && length >= 0x20 ? BitConverter.ToUInt32(raw, at + 0x1C) & 0x7FFFFFFF
                    : (sizeField & 0x8000) != 0 ? (sizeField & 0x7FFF) / 1024 : sizeField;
                var memoryType = raw[at + 0x12] switch
                {
                    0x12 => "DDR SDRAM", 0x13 => "DDR2 SDRAM", 0x18 => "DDR3 SDRAM", 0x1A => "DDR4 SDRAM", 0x1B => "LPDDR SDRAM",
                    0x1C => "LPDDR2 SDRAM", 0x1D => "LPDDR3 SDRAM", 0x1E => "LPDDR4 SDRAM", 0x22 => "DDR5 SDRAM", 0x23 => "LPDDR5 SDRAM",
                    var t => $"Unknown (0x{t:x2})",
                };
                var form = raw[at + 0x0E] switch { 0x09 => "DIMM", 0x0D => "SO-DIMM", 0x0B => "Row of chips (soldered)", 0x0F => "FB-DIMM", var f => $"Other (0x{f:x2})" };
                result.Add(new Dictionary<string, string>
                {
                    ["Locator"] = Str(0x10), ["Bank"] = Str(0x11), ["Type"] = memoryType, ["FormFactor"] = form,
                    ["Speed"] = Word(0x15) is var sp and > 0 ? $"{sp} MT/s" : "Unknown",
                    ["Configured"] = length >= 0x22 && Word(0x20) is var cs and > 0 ? $"{cs} MT/s" : "Unknown",
                    ["Size"] = megabytes >= 1024 && megabytes % 1024 == 0 ? $"{megabytes / 1024} GB" : $"{megabytes} MB",
                    ["Width"] = $"{Word(0x0A)} bits / {Word(0x08)} bits",
                    ["Rank"] = length >= 0x1C && (raw[at + 0x1B] & 0xF) is var rank and > 0 ? rank.ToString() : "Unknown",
                    ["Voltage"] = length >= 0x28 && Word(0x26) is var mv and > 0 ? $"{mv / 1000.0:0.0##} V" : "Unknown",
                    ["Manufacturer"] = Str(0x17), ["Serial"] = Str(0x18), ["Part"] = Str(0x1A),
                });
            }
            if (type == 127) break;
            at = next;
        }
        return result;
    }

    [DllImport("kernel32.dll")]
    static extern uint GetSystemFirmwareTable(uint provider, uint id, byte[] buffer, uint size);
}
