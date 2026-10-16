using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace WinTools;

// lume: create and manage virtual machines, on Hyper-V instead of Apple's Virtualization framework.
// Every subcommand drives Hyper-V's PowerShell module (New-VM, Start-VM, Get-VM ...); disks live in
// %UserProfile%\.lume\<name> like lume's VM bundles, and the guest OS lume records goes in the VM's
// notes. `lume serve` exposes the same operations as lume's HTTP API. macOS guests, IPSW restore
// images and lume's prebuilt images are Apple-silicon-only, so those commands explain that and exit 1.
// Windows Home has no Hyper-V; every VM command says so cleanly there.
static class Lume
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("lume", "Create and manage virtual machines (Hyper-V)", Run);
    }

    const string Usage = """
        OVERVIEW: A lightweight CLI and local API server to build, run and manage virtual machines (Hyper-V).

        USAGE: lume <subcommand>

        OPTIONS:
          --version               Show the version.
          -h, --help              Show help information.

        SUBCOMMANDS:
          create <name>           Create a new virtual machine
                                  [--os linux|windows] [--cpu 4] [--memory 8GB] [--disk-size 50GB] [--display 1024x768] [--storage <dir>]
          run <name>              Run a virtual machine [--no-display] [--mount <iso>]
          stop <name>             Stop a virtual machine
          ls, list                List virtual machines [-f json]
          get <name>              Get detailed information about a virtual machine [-f json]
          set <name>              Set new values for CPU, memory, disk size and display
          delete <name>           Delete a virtual machine [--force]
          clone <name> <new-name> Clone an existing virtual machine
          serve                   Start the lume API server [--port 7777]
          pull, push, images, ipsw, setup, prune
                                  lume's macOS images and IPSW restores (Apple silicon only)
        """;

    static readonly string NotFound = "Virtual machine not found";

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help") { Console.WriteLine(Usage); return args.Length == 0 ? 1 : 0; }
        if (args[0] is "--version" or "version") { Console.WriteLine(Commands.Version); return 0; }
        var rest = args[1..];
        if (rest.Any(a => a is "-h" or "--help")) { Console.WriteLine(Usage); return 0; }
        return args[0] switch
        {
            "create" => Create(rest),
            "run" => RunVm(rest),
            "stop" => Stop(rest),
            "ls" or "list" => List(rest),
            "get" => Get(rest),
            "set" => Set(rest),
            "delete" or "rm" => Delete(rest),
            "clone" => Clone(rest),
            "serve" => Serve(rest),
            "setup" => Setup(rest),
            "pull" or "push" or "images" or "prune" => Fail($"lume {args[0]}: lume's images are Apple Virtualization bundles for Apple silicon, which Hyper-V can't boot; create a VM and install from an ISO with `lume run <name> --mount <iso>`"),
            "ipsw" => Fail("lume ipsw: IPSW restore images install macOS guests, which only run on Apple silicon Macs"),
            "config" => Fail("lume config: VM disks are kept in %UserProfile%\\.lume (use --storage on create for elsewhere); Hyper-V's own defaults are in Hyper-V Manager"),
            _ => throw new UsageException($"unknown subcommand '{args[0]}' (see lume --help)"),
        };
    }

    static int Fail(string message) { Console.Error.WriteLine(message); return 1; }

    // ---- options ----

    static string Option(string[] args, params string[] names)
    {
        for (var i = 0; i < args.Length; i++)
        {
            foreach (var name in names)
            {
                if (args[i] == name) return i + 1 < args.Length ? args[i + 1] : throw new UsageException($"{name} takes a value");
                if (args[i].StartsWith(name + "=", StringComparison.Ordinal)) return args[i][(name.Length + 1)..];
            }
        }
        return null;
    }

    static List<string> Positionals(string[] args, params string[] valued)
    {
        var list = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (valued.Contains(args[i])) { i++; continue; }
            if (args[i].StartsWith('-')) continue;
            list.Add(args[i]);
        }
        return list;
    }

    static string Name(string[] args, params string[] valued) =>
        Positionals(args, valued).FirstOrDefault() ?? throw new UsageException("missing expected argument '<name>'");

    // "8GB", "512MB", "50G", "1TB" -> bytes
    internal static long ParseSize(string text)
    {
        var m = System.Text.RegularExpressions.Regex.Match(text.Trim(), @"^(\d+(?:\.\d+)?)\s*([KMGT]?)i?B?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!m.Success) throw new UsageException($"invalid size '{text}' (e.g. 8GB, 512MB)");
        var value = double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        var unit = m.Groups[2].Value.ToUpperInvariant() switch { "K" => 1L << 10, "M" => 1L << 20, "G" => 1L << 30, "T" => 1L << 40, _ => 1L };
        return (long)(value * unit);
    }

    internal static (int W, int H) ParseDisplay(string text)
    {
        var parts = text.ToLowerInvariant().Split('x');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var w) || !int.TryParse(parts[1], out var h) || w < 640 || h < 480)
            throw new UsageException($"invalid display '{text}' (e.g. 1024x768)");
        return (w, h);
    }

    static string FormatBytes(long bytes) => bytes >= 1L << 30 ? $"{bytes / (double)(1L << 30):0.##}G" : $"{bytes / (double)(1L << 20):0}M";

    static string Storage(string custom, string name) =>
        Path.Combine(custom != null ? Path.GetFullPath(custom) : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".lume"), name);

    // ---- Hyper-V ----

    // Hyper-V is there when its management service is installed; its PowerShell module is a separate feature.
    static void RequireHyperV()
    {
        using var vmms = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\vmms");
        if (vmms == null)
            throw new InvalidOperationException("Hyper-V is not enabled on this PC (Windows Home doesn't include it; on Pro, Enterprise or Education enable the Microsoft-Hyper-V feature)");
        if (!Directory.Exists(Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\Modules\Hyper-V")))
            throw new InvalidOperationException("the Hyper-V PowerShell module isn't installed (enable the Microsoft-Hyper-V-Management-PowerShell feature)");
    }

    static string Q(string s) => "'" + s.Replace("'", "''") + "'";

    // Runs a script in Windows PowerShell with errors as terminating; failures become one-line errors.
    static string Ps(string script)
    {
        RequireHyperV();
        var start = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe"))
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            CreateNoWindow = true,
        };
        foreach (var a in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-Command", "-" }) start.ArgumentList.Add(a);
        start.RedirectStandardInput = true;
        using var p = Process.Start(start);
        p.StandardInput.Write("$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue'\n" +
                              "[Console]::OutputEncoding = [Text.Encoding]::UTF8\n" +
                              "try {\n" + script + "\n} catch { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }\n");
        p.StandardInput.Close();
        var err = p.StandardError.ReadToEndAsync();
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0)
        {
            var message = err.Result.Trim().Split('\n').FirstOrDefault()?.Trim() ?? "PowerShell failed";
            if (message.Contains("unable to find a virtual machine", StringComparison.OrdinalIgnoreCase)) message = NotFound;
            if (message.Contains("required permission", StringComparison.OrdinalIgnoreCase) || message.Contains("authorization policy", StringComparison.OrdinalIgnoreCase))
                message += " (run from an administrator prompt or join the Hyper-V Administrators group)";
            throw new InvalidOperationException(message);
        }
        return output;
    }

    sealed class Vm
    {
        public string Name, Os, Status, Display, Ip, Storage;
        public int Cpu;
        public long Memory, Disk, DiskUsed;
    }

    const string VmQuery = """
        $result = @(foreach ($vm in $vms) {
          $disk = $vm | Get-VMHardDiskDrive | Select-Object -First 1
          $size = 0; $used = 0
          if ($disk -and $disk.Path -and (Test-Path -LiteralPath $disk.Path)) { try { $vhd = Get-VHD -Path $disk.Path; $size = $vhd.Size; $used = $vhd.FileSize } catch { } }
          $video = $null; try { $video = Get-VMVideo -VM $vm } catch { }
          $ip = @($vm.NetworkAdapters | ForEach-Object { $_.IPAddresses } | Where-Object { $_ -match '^\d+\.\d+\.\d+\.\d+$' }) | Select-Object -First 1
          [pscustomobject]@{
            name = $vm.Name; state = "$($vm.State)"; cpu = $vm.ProcessorCount; memory = $vm.MemoryStartup
            disk = $size; diskUsed = $used; notes = $vm.Notes; ip = "$ip"
            display = $(if ($video) { "$($video.HorizontalResolution)x$($video.VerticalResolution)" } else { "" })
            path = $(if ($disk) { Split-Path -Parent $disk.Path } else { $vm.Path })
          }
        })
        ConvertTo-Json -InputObject $result -Depth 3 -Compress
        """;

    static List<Vm> Query(string name = null)
    {
        var select = name == null ? "$vms = @(Get-VM)" : $"$vms = @(Get-VM -Name {Q(name)})";
        var json = Ps(select + "\n" + VmQuery).Trim();
        var list = new List<Vm>();
        if (json.Length == 0) return list;
        using var doc = JsonDocument.Parse(json);
        foreach (var e in doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement.EnumerateArray().ToList() : [doc.RootElement])
        {
            string S(string k) => e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : "";
            long L(string k) => e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : 0;
            var notes = S("notes");
            var os = System.Text.RegularExpressions.Regex.Match(notes, @"lume:\s*os=(\w+)") is { Success: true } m ? m.Groups[1].Value : "linux";
            list.Add(new Vm
            {
                Name = S("name"), Os = os, Cpu = (int)L("cpu"), Memory = L("memory"), Disk = L("disk"), DiskUsed = L("diskUsed"),
                Display = S("display"), Ip = S("ip"), Storage = S("path"),
                Status = S("state") switch { "Running" => "running", "Off" => "stopped", "Saved" => "suspended", "Paused" => "paused", var s => s.ToLowerInvariant() },
            });
        }
        return list;
    }

    static Vm Find(string name)
    {
        RequireHyperV();
        try { return Query(name).FirstOrDefault() ?? throw new InvalidOperationException(NotFound); }
        catch (InvalidOperationException e) when (e.Message.Contains("not find", StringComparison.OrdinalIgnoreCase) || e.Message.Contains("unable to find", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(NotFound);
        }
    }

    // ---- subcommands ----

    static int List(string[] args)
    {
        var vms = Query();
        if (Option(args, "-f", "--format") == "json") { Console.WriteLine(Json(vms)); return 0; }
        if (vms.Count == 0) { Console.WriteLine("No virtual machines found"); return 0; }
        var rows = new List<string[]> { new[] { "name", "os", "cpu", "memory", "disk", "display", "status", "storage", "ip" } };
        rows.AddRange(vms.Select(v => new[] { v.Name, v.Os, v.Cpu.ToString(), FormatBytes(v.Memory), $"{FormatBytes(v.DiskUsed)}/{FormatBytes(v.Disk)}", v.Display, v.Status, v.Storage, v.Ip.Length > 0 ? v.Ip : "-" }));
        Container.PrintTable(rows);
        return 0;
    }

    static int Get(string[] args)
    {
        var vm = Find(Name(args, "-f", "--format"));
        if (Option(args, "-f", "--format") == "json") { Console.WriteLine(Json([vm])); return 0; }
        var rows = new List<string[]> { new[] { "name", "os", "cpu", "memory", "disk", "display", "status", "storage", "ip" } };
        rows.Add([vm.Name, vm.Os, vm.Cpu.ToString(), FormatBytes(vm.Memory), $"{FormatBytes(vm.DiskUsed)}/{FormatBytes(vm.Disk)}", vm.Display, vm.Status, vm.Storage, vm.Ip.Length > 0 ? vm.Ip : "-"]);
        Container.PrintTable(rows);
        return 0;
    }

    static string Json(List<Vm> vms)
    {
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            w.WriteStartArray();
            foreach (var v in vms)
            {
                w.WriteStartObject();
                w.WriteString("name", v.Name);
                w.WriteString("os", v.Os);
                w.WriteNumber("cpuCount", v.Cpu);
                w.WriteNumber("memorySize", v.Memory);
                w.WriteStartObject("diskSize");
                w.WriteNumber("allocated", v.DiskUsed);
                w.WriteNumber("total", v.Disk);
                w.WriteEndObject();
                w.WriteString("display", v.Display);
                w.WriteString("status", v.Status);
                w.WriteString("locationName", v.Storage);
                if (v.Ip.Length > 0) w.WriteString("ipAddress", v.Ip); else w.WriteNull("ipAddress");
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    static readonly string[] CreateValued = ["--os", "--cpu", "--memory", "--disk-size", "--display", "--ipsw", "--storage", "--registry", "--organization", "--unattended"];

    static int Create(string[] args) { CreateVm(Name(args, CreateValued), Option(args, "--os") ?? "linux", Option(args, "--cpu"), Option(args, "--memory"), Option(args, "--disk-size"), Option(args, "--display"), Option(args, "--storage"), Option(args, "--ipsw")); return 0; }

    static void CreateVm(string name, string os, string cpu, string memory, string diskSize, string display, string storage, string ipsw)
    {
        os = os.ToLowerInvariant();
        if (os == "macos" || ipsw != null) throw new InvalidOperationException("macOS virtual machines need Apple's Virtualization framework on an Apple silicon Mac; Hyper-V runs Linux and Windows guests");
        if (os is not ("linux" or "windows")) throw new UsageException($"unsupported os '{os}' (linux or windows)");
        if (!System.Text.RegularExpressions.Regex.IsMatch(name, "^[A-Za-z0-9][A-Za-z0-9_.-]*$")) throw new UsageException($"invalid VM name '{name}'");
        var cpus = int.TryParse(cpu ?? "4", out var c) && c > 0 ? c : throw new UsageException($"invalid cpu count '{cpu}'");
        var mem = ParseSize(memory ?? "8GB");
        var disk = ParseSize(diskSize ?? "50GB");
        var (w, h) = ParseDisplay(display ?? "1024x768");
        var dir = Storage(storage, name);
        RequireHyperV();
        var template = os == "linux" ? "MicrosoftUEFICertificateAuthority" : "MicrosoftWindows";
        Ps($$"""
            if (Get-VM -Name {{Q(name)}} -ErrorAction SilentlyContinue) { throw "Virtual machine already exists: {{name.Replace("\"", "")}}" }
            New-Item -ItemType Directory -Force -Path {{Q(dir)}} | Out-Null
            $switch = Get-VMSwitch -Name 'Default Switch' -ErrorAction SilentlyContinue | Select-Object -First 1
            if (-not $switch) { $switch = Get-VMSwitch | Select-Object -First 1 }
            $params = @{ Name = {{Q(name)}}; Generation = 2; MemoryStartupBytes = {{mem}}; NewVHDPath = {{Q(Path.Combine(dir, "disk.vhdx"))}}; NewVHDSizeBytes = {{disk}}; Path = {{Q(dir)}} }
            if ($switch) { $params.SwitchName = $switch.Name }
            $vm = New-VM @params
            Set-VMProcessor -VM $vm -Count {{cpus}}
            Set-VM -VM $vm -Notes 'lume: os={{os}}' -AutomaticCheckpointsEnabled $false
            Set-VMFirmware -VM $vm -SecureBootTemplate {{template}}
            if (Get-Command Set-VMVideo -ErrorAction SilentlyContinue) { Set-VMVideo -VM $vm -HorizontalResolution {{w}} -VerticalResolution {{h}} -ResolutionType Single }
            """);
        Console.WriteLine($"Virtual machine {name} created successfully (disk: {Path.Combine(dir, "disk.vhdx")})");
    }

    static readonly string[] RunValued = ["--shared-dir", "--mount", "--vnc-port", "--registry", "--organization", "--storage", "--usb-storage"];

    static int RunVm(string[] args)
    {
        var name = Name(args, RunValued);
        if (Option(args, "--shared-dir") != null)
            return Fail("lume run --shared-dir: Hyper-V has no virtio-fs; share the folder over SMB (Windows file sharing) and mount it in the guest");
        if (args.Contains("--recovery-mode")) return Fail("lume run --recovery-mode: macOS recovery mode needs an Apple silicon Mac");
        StartVm(name, Option(args, "--mount"));
        Console.WriteLine($"Virtual machine {name} is running");
        if (!args.Contains("--no-display"))
            Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "vmconnect.exe")) { ArgumentList = { "localhost", name }, UseShellExecute = false });
        return 0;
    }

    static void StartVm(string name, string iso)
    {
        Find(name);
        var mount = iso == null ? "" : $$"""
            $iso = {{Q(Path.GetFullPath(iso))}}
            if (-not (Test-Path -LiteralPath $iso)) { throw "ISO not found: $iso" }
            $dvd = Get-VMDvdDrive -VMName {{Q(name)}} | Select-Object -First 1
            if ($dvd) { Set-VMDvdDrive -VMName {{Q(name)}} -ControllerNumber $dvd.ControllerNumber -ControllerLocation $dvd.ControllerLocation -Path $iso; $dvd = Get-VMDvdDrive -VMName {{Q(name)}} | Select-Object -First 1 }
            else { $dvd = Add-VMDvdDrive -VMName {{Q(name)}} -Path $iso -Passthru }
            Set-VMFirmware -VMName {{Q(name)}} -FirstBootDevice $dvd
            """;
        Ps(mount + $"\nStart-VM -Name {Q(name)}");
    }

    static int Stop(string[] args)
    {
        var name = Name(args);
        Find(name);
        Ps($"Stop-VM -Name {Q(name)} -Force");
        Console.WriteLine($"Virtual machine {name} stopped");
        return 0;
    }

    static int Set(string[] args)
    {
        var name = Name(args, "--cpu", "--memory", "--disk-size", "--display", "--storage");
        Find(name);
        var script = new StringBuilder();
        if (Option(args, "--cpu") is { } cpu) script.AppendLine($"Set-VMProcessor -VMName {Q(name)} -Count {(int.TryParse(cpu, out var c) && c > 0 ? c : throw new UsageException($"invalid cpu count '{cpu}'"))}");
        if (Option(args, "--memory") is { } memory) script.AppendLine($"Set-VMMemory -VMName {Q(name)} -StartupBytes {ParseSize(memory)}");
        if (Option(args, "--disk-size") is { } disk)
            script.AppendLine($"$d = Get-VMHardDiskDrive -VMName {Q(name)} | Select-Object -First 1; if (-not $d) {{ throw 'the VM has no disk' }}; Resize-VHD -Path $d.Path -SizeBytes {ParseSize(disk)}");
        if (Option(args, "--display") is { } display)
        {
            var (w, h) = ParseDisplay(display);
            script.AppendLine($"Set-VMVideo -VMName {Q(name)} -HorizontalResolution {w} -VerticalResolution {h} -ResolutionType Single");
        }
        if (script.Length == 0) throw new UsageException("nothing to set (use --cpu, --memory, --disk-size or --display)");
        Ps(script.ToString());
        Console.WriteLine($"Virtual machine {name} updated");
        return 0;
    }

    static int Delete(string[] args)
    {
        var name = Name(args, "--storage");
        var vm = Find(name);
        if (!args.Contains("--force"))
        {
            Console.Write($"Are you sure you want to delete the virtual machine {name}? [y/N] ");
            if (Console.ReadLine()?.Trim().ToLowerInvariant() is not ("y" or "yes")) { Console.WriteLine("Deletion cancelled"); return 0; }
        }
        DeleteVm(name, vm);
        Console.WriteLine($"Virtual machine {name} deleted");
        return 0;
    }

    // Removes the VM and its disks; the folder too when it is lume's own (holds nothing else).
    static void DeleteVm(string name, Vm vm)
    {
        Ps($$"""
            $vm = Get-VM -Name {{Q(name)}}
            if ($vm.State -ne 'Off') { Stop-VM -VM $vm -TurnOff -Force }
            $disks = @(Get-VMHardDiskDrive -VM $vm | ForEach-Object { $_.Path })
            Remove-VM -VM $vm -Force
            foreach ($d in $disks) { if ($d -and (Test-Path -LiteralPath $d)) { Remove-Item -LiteralPath $d -Force } }
            """);
        var lumeRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".lume");
        if (vm.Storage.StartsWith(lumeRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(vm.Storage))
            Directory.Delete(vm.Storage, true);
    }

    static int Clone(string[] args)
    {
        var names = Positionals(args, "--source-storage", "--dest-storage");
        if (names.Count != 2) throw new UsageException("clone takes <name> <new-name>");
        CloneVm(names[0], names[1], Option(args, "--dest-storage"));
        Console.WriteLine($"Virtual machine {names[0]} cloned to {names[1]}");
        return 0;
    }

    static void CloneVm(string source, string target, string storage)
    {
        Find(source);
        var dir = Storage(storage, target);
        Ps($$"""
            if (Get-VM -Name {{Q(target)}} -ErrorAction SilentlyContinue) { throw "Virtual machine already exists: {{target.Replace("\"", "")}}" }
            $tmp = Join-Path $env:TEMP ('lume-clone-' + [guid]::NewGuid())
            try {
              Export-VM -Name {{Q(source)}} -Path $tmp
              $vmcx = Get-ChildItem -LiteralPath $tmp -Recurse -Filter *.vmcx | Select-Object -First 1
              New-Item -ItemType Directory -Force -Path {{Q(dir)}} | Out-Null
              $vm = Import-VM -Path $vmcx.FullName -Copy -GenerateNewId -VirtualMachinePath {{Q(dir)}} -VhdDestinationPath {{Q(dir)}} -SnapshotFilePath {{Q(dir)}} -SmartPagingFilePath {{Q(dir)}}
              Rename-VM -VM $vm -NewName {{Q(target)}}
            } finally { if (Test-Path -LiteralPath $tmp) { Remove-Item -LiteralPath $tmp -Recurse -Force } }
            """);
    }

    static int Setup(string[] args)
    {
        var name = Name(args, "--unattended", "--storage", "--vnc-port");
        Find(name);
        return Fail("lume setup: unattended setup drives macOS's Setup Assistant, which only runs in macOS guests on Apple silicon");
    }

    // ---- lume serve: lume's HTTP API over Hyper-V ----

    static int Serve(string[] args)
    {
        var port = int.TryParse(Option(args, "--port", "-p") ?? "7777", out var p) && p is > 0 and < 65536 ? p : throw new UsageException("invalid --port");
        var listener = new TcpListener(IPAddress.Loopback, port);
        try { listener.Start(); }
        catch (SocketException e) { throw new InvalidOperationException($"can't listen on port {port}: {e.Message}"); }
        Console.WriteLine($"Starting lume API server on http://localhost:{port}/lume");
        using var stop = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
        while (!stop.IsCancellationRequested)
        {
            TcpClient client;
            try { client = listener.AcceptTcpClientAsync(stop.Token).AsTask().GetAwaiter().GetResult(); }
            catch (OperationCanceledException) { break; }
            _ = Task.Run(() => Handle(client));
        }
        listener.Stop();
        return 0;
    }

    static void Handle(TcpClient client)
    {
        using var c = client;
        var stream = c.GetStream();
        try
        {
            var reader = new StreamReader(stream, Encoding.UTF8, false, 8192, leaveOpen: true);
            var requestLine = reader.ReadLine();
            if (requestLine == null) return;
            var parts = requestLine.Split(' ');
            if (parts.Length < 2) { Respond(stream, 400, Error("bad request")); return; }
            var length = 0;
            string line;
            while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) int.TryParse(line[15..].Trim(), out length);
            var body = new char[length];
            var read = 0;
            while (read < length) { var n = reader.Read(body, read, length - read); if (n <= 0) break; read += n; }
            var (status, json) = Route(parts[0], Uri.UnescapeDataString(parts[1].Split('?')[0]).TrimEnd('/'), new string(body, 0, read));
            Respond(stream, status, json);
        }
        catch (IOException) { }
    }

    static (int, string) Route(string method, string path, string body)
    {
        try
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2 || segments[0] != "lume") return (404, Error("not found"));
            JsonElement Body() => body.Length > 0 ? JsonDocument.Parse(body).RootElement : default;
            string Field(JsonElement e, string name) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) ? (v.ValueKind == JsonValueKind.String ? v.GetString() : v.GetRawText()) : null;
            switch (segments[1], segments.Length, method)
            {
                case ("vms", 2, "GET"): return (200, Json(Query()));
                case ("vms", 2, "POST"):
                {
                    var b = Body();
                    CreateVm(Field(b, "name") ?? throw new UsageException("name is required"), Field(b, "os") ?? "linux", Field(b, "cpu"), Field(b, "memory"),
                        Field(b, "diskSize"), Field(b, "display"), Field(b, "storage"), Field(b, "ipsw"));
                    return (200, Json([Find(Field(b, "name"))]).Trim()[1..^1].Trim());
                }
                case ("vms", 3, "POST") when segments[2] == "clone":
                {
                    var b = Body();
                    CloneVm(Field(b, "name") ?? throw new UsageException("name is required"), Field(b, "newName") ?? throw new UsageException("newName is required"), Field(b, "destStorage"));
                    return (200, Json([Find(Field(b, "newName"))]).Trim()[1..^1].Trim());
                }
                case ("vms", 3, "GET"): return (200, Json([Find(segments[2])]).Trim()[1..^1].Trim());
                case ("vms", 3, "DELETE"): DeleteVm(segments[2], Find(segments[2])); return (200, "{\"message\":\"Virtual machine deleted\"}");
                case ("vms", 3, "PATCH"):
                {
                    var b = Body();
                    var args = new List<string> { segments[2] };
                    foreach (var (key, flag) in new[] { ("cpu", "--cpu"), ("memory", "--memory"), ("diskSize", "--disk-size"), ("display", "--display") })
                        if (Field(b, key) is { } v) args.AddRange([flag, v]);
                    Set(args.ToArray());
                    return (200, Json([Find(segments[2])]).Trim()[1..^1].Trim());
                }
                case ("vms", 4, "POST") when segments[3] == "run":
                {
                    var b = Body();
                    if (Field(b, "sharedDirectories") is { } shared && shared != "[]") return (400, Error("shared directories need virtio-fs, which Hyper-V doesn't have"));
                    StartVm(segments[2], Field(b, "mount"));
                    return (202, "{\"message\":\"VM start initiated\"}");
                }
                case ("vms", 4, "POST") when segments[3] == "stop":
                    Find(segments[2]);
                    Ps($"Stop-VM -Name {Q(segments[2])} -Force");
                    return (200, "{\"message\":\"VM stopped\"}");
                case ("images" or "pull" or "push" or "ipsw" or "prune", _, _):
                    return (400, Error("lume images and IPSW restores are for Apple silicon Macs only"));
                default: return (404, Error("not found"));
            }
        }
        catch (InvalidOperationException e) { return (e.Message == NotFound ? 404 : 500, Error(e.Message)); }
        catch (UsageException e) { return (400, Error(e.Message)); }
        catch (JsonException e) { return (400, Error("invalid JSON: " + e.Message)); }
    }

    static string Error(string message) => "{\"message\":\"" + JsonEncodedText.Encode(message) + "\"}";

    static void Respond(Stream stream, int status, string json)
    {
        var body = Encoding.UTF8.GetBytes(json);
        var reason = status switch { 200 => "OK", 202 => "Accepted", 400 => "Bad Request", 404 => "Not Found", _ => "Internal Server Error" };
        var head = $"HTTP/1.1 {status} {reason}\r\nContent-Type: application/json\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n";
        stream.Write(Encoding.ASCII.GetBytes(head));
        stream.Write(body);
    }
}
