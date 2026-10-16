using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace WinTools;

// kdoctor: Kotlin Multiplatform environment diagnostics, checking what a Windows machine needs for
// the Android (and desktop/web) side of a KMP project: the OS, a JDK and JAVA_HOME, the Android SDK,
// Android Studio with its Kotlin plugins, and Gradle. Xcode and CocoaPods only exist on macOS, so
// those sections say iOS targets build on a Mac rather than failing the check.
static class Kdoctor
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("kdoctor", "Environment diagnostics for Kotlin Multiplatform development", Run);
    }

    const string Usage = """
        Usage: kdoctor [OPTIONS]

        Options:
          -v, --verbose   Report all details of the diagnostics
          --team-ids      List Apple development team IDs from the code-signing certificates
                          in the Windows certificate store
          --version       Print the version and exit
          -h, --help      Show this message and exit
        """;

    enum Result { Success, Warning, Failure, Info }

    sealed class Section(string title)
    {
        public string Title = title;
        public Result Result = Result.Success;
        public readonly List<(Result Kind, string Text)> Lines = [];

        public void Add(Result kind, string text)
        {
            Lines.Add((kind, text));
            if (kind == Result.Failure || (kind == Result.Warning && Result == Result.Success)) Result = kind;
        }
    }

    static int Run(string[] args)
    {
        var verbose = false;
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help": Console.WriteLine(Usage); return 0;
                case "--version": Console.WriteLine($"{Commands.Version} (wintools)"); return 0;
                case "-v" or "--verbose": verbose = true; break;
                case "--team-ids": return TeamIds();
                default: throw new UsageException($"no such option: {arg}\n{Usage}");
            }
        }

        Console.WriteLine(verbose ? "Environment diagnose:" : "Environment diagnose (to see all details, use -v option):");
        var sections = new[] { SystemSection(), JavaSection(), AndroidSdkSection(), AndroidStudioSection(), GradleSection(), XcodeSection(), CocoaPodsSection() };
        foreach (var section in sections) Print(section, verbose);

        Console.WriteLine();
        Console.WriteLine("Conclusion:");
        if (sections.Any(s => s.Result == Result.Failure))
        {
            Console.WriteLine("  [x] KDoctor has diagnosed one or more problems while checking your environment.");
            Console.WriteLine("    Please check the output above and fix the issues marked with [x].");
        }
        else if (sections.Any(s => s.Result == Result.Warning))
        {
            Console.WriteLine("  [!] Your system is ready for Kotlin Multiplatform development of the Android, desktop and web targets,");
            Console.WriteLine("    but there are warnings above (marked with [!]). iOS targets need a Mac with Xcode.");
        }
        else
        {
            Console.WriteLine("  [v] Your system is ready for Kotlin Multiplatform development of the Android, desktop and web targets!");
            Console.WriteLine("    iOS targets need a Mac with Xcode.");
        }
        return 0;
    }

    static string Mark(Result r) => r switch { Result.Success => "[v]", Result.Warning => "[!]", Result.Failure => "[x]", _ => "[i]" };

    static void Print(Section section, bool verbose)
    {
        Console.WriteLine($"{Mark(section.Result)} {section.Title}");
        foreach (var (kind, text) in section.Lines)
        {
            // Without -v only the problems are shown, like kdoctor.
            if (!verbose && kind is Result.Success or Result.Info) continue;
            var lines = text.Split('\n');
            var bullet = kind switch { Result.Failure => "x", Result.Warning => "!", _ => ">" };
            Console.WriteLine($"  {bullet} {lines[0]}");
            foreach (var line in lines.Skip(1)) Console.WriteLine($"    {line}");
        }
    }

    static Section SystemSection()
    {
        var s = new Section("Operation System");
        using var nt = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        var product = nt?.GetValue("ProductName") as string ?? "Windows";
        var build = Environment.OSVersion.Version.Build;
        // Windows 11 still reports "Windows 10" in ProductName; the build number tells them apart.
        if (build >= 22000) product = product.Replace("Windows 10", "Windows 11");
        var display = nt?.GetValue("DisplayVersion") as string;
        using var cpu = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
        var cpuName = (cpu?.GetValue("ProcessorNameString") as string ?? System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString()).Trim();
        s.Add(Result.Success, $"Version OS: {product}{(display != null ? " " + display : "")} (build {Environment.OSVersion.Version})\nCPU: {cpuName}");
        if (build < 17763) s.Add(Result.Warning, "Android Studio and current JDKs need Windows 10 1809 or later");
        return s;
    }

    static Section JavaSection()
    {
        var s = new Section("Java");
        var onPath = FindOnPath("java.exe");
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (onPath == null && string.IsNullOrEmpty(javaHome))
        {
            s.Add(Result.Failure, "Java not found\nGet a JDK from https://adoptium.net or install the openjdk formula, then set JAVA_HOME");
            return s;
        }
        if (onPath != null)
        {
            var version = JavaVersion(onPath);
            s.Add(Result.Success, $"Java ({version ?? "unknown version"})\nLocation: {onPath}");
        }
        else s.Add(Result.Warning, "java is not on PATH");

        if (string.IsNullOrEmpty(javaHome))
            s.Add(Result.Warning, "JAVA_HOME is not set\nGradle uses JAVA_HOME to find the JDK; set it to your JDK's directory");
        else if (!File.Exists(Path.Combine(javaHome, "bin", "java.exe")))
            s.Add(Result.Failure, $"JAVA_HOME={javaHome} does not contain bin\\java.exe");
        else
        {
            var homeJava = Path.Combine(javaHome, "bin", "java.exe");
            s.Add(Result.Success, $"JAVA_HOME: {javaHome}");
            if (onPath != null && !SamePath(Path.GetDirectoryName(onPath), Path.GetDirectoryName(homeJava)))
                s.Add(Result.Warning, $"JAVA_HOME ({JavaVersion(homeJava) ?? javaHome}) is not the java on PATH\n" +
                                      "Gradle and the IDE may use different JDKs");
        }
        return s;
    }

    static Section AndroidSdkSection()
    {
        var s = new Section("Android SDK");
        var env = Environment.GetEnvironmentVariable("ANDROID_HOME") ?? Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
        var sdk = env;
        if (string.IsNullOrEmpty(sdk))
        {
            var local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk");
            if (Directory.Exists(local)) sdk = local;
        }
        if (string.IsNullOrEmpty(sdk) || !Directory.Exists(sdk))
        {
            s.Add(Result.Failure, "Android SDK not found\nInstall it from Android Studio's SDK Manager and set ANDROID_HOME");
            return s;
        }
        s.Add(Result.Success, $"Location: {sdk}");
        if (string.IsNullOrEmpty(env)) s.Add(Result.Warning, "ANDROID_HOME is not set (found the SDK in its default location)");
        var platforms = Path.Combine(sdk, "platforms");
        var installed = Directory.Exists(platforms) ? Directory.GetDirectories(platforms).Select(Path.GetFileName).Order().ToList() : [];
        if (installed.Count == 0) s.Add(Result.Warning, "No Android platforms are installed (sdkmanager \"platforms;android-35\")");
        else s.Add(Result.Info, "Platforms: " + string.Join(", ", installed));
        if (!Directory.Exists(Path.Combine(sdk, "platform-tools"))) s.Add(Result.Warning, "platform-tools (adb) are not installed");
        return s;
    }

    static Section AndroidStudioSection()
    {
        var s = new Section("Android Studio");
        var installs = FindAndroidStudios();
        if (installs.Count == 0)
        {
            s.Add(Result.Failure, "No Android Studio installation found\nGet it from https://developer.android.com/studio (winget install Google.AndroidStudio)");
            return s;
        }
        foreach (var (home, version, dataName) in installs)
        {
            var lines = $"Android Studio ({version})\nLocation: {home}";
            var bundledKotlin = ReadPluginVersion(Path.Combine(home, "plugins", "Kotlin"));
            lines += "\nBundled Kotlin Plugin: " + (bundledKotlin ?? "not found");
            var configPlugins = dataName == null ? null : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Google", dataName, "plugins");
            string kmp = null;
            if (configPlugins != null && Directory.Exists(configPlugins))
                foreach (var dir in Directory.GetDirectories(configPlugins))
                {
                    var name = Path.GetFileName(dir);
                    if (name.Contains("multiplatform", StringComparison.OrdinalIgnoreCase) || name.Contains("kmm", StringComparison.OrdinalIgnoreCase))
                        kmp = ReadPluginVersion(dir) ?? name;
                }
            lines += "\nKotlin Multiplatform Plugin: " + (kmp ?? "not installed");
            s.Add(Result.Success, lines);
            if (bundledKotlin == null) s.Add(Result.Warning, "The Kotlin plugin is missing from this Android Studio");
            if (kmp == null) s.Add(Result.Info, "Install the Kotlin Multiplatform plugin from Settings > Plugins for project wizards and run configurations");
        }
        return s;
    }

    static Section GradleSection()
    {
        var s = new Section("Gradle");
        var wrapper = Path.Combine("gradle", "wrapper", "gradle-wrapper.properties");
        if (File.Exists(wrapper))
        {
            var url = Regex.Match(File.ReadAllText(wrapper), @"distributionUrl=.*gradle-([\d.]+(?:-rc-\d+)?)-").Groups[1].Value;
            s.Add(Result.Success, $"Gradle wrapper ({(url.Length > 0 ? url : "unknown version")}) in {Path.GetFullPath(".")}");
        }
        var gradle = FindOnPath("gradle.bat") ?? FindOnPath("gradle.exe");
        if (gradle != null) s.Add(Result.Success, $"Gradle on PATH: {gradle}");
        if (!File.Exists(wrapper) && gradle == null)
            s.Add(Result.Info, "No gradle on PATH; Kotlin Multiplatform projects build with their own gradlew wrapper");
        return s;
    }

    static Section XcodeSection()
    {
        var s = new Section("Xcode");
        s.Add(Result.Warning, "Xcode runs only on macOS\niOS targets of a Kotlin Multiplatform project are built on a Mac; " +
                              "the Android, desktop (JVM) and web targets build on Windows");
        return s;
    }

    static Section CocoaPodsSection()
    {
        var s = new Section("CocoaPods");
        s.Add(Result.Warning, "CocoaPods integration needs Xcode, which runs only on macOS\n" +
                              "Kotlin's CocoaPods tasks are skipped on Windows; run pod install on a Mac");
        return s;
    }

    // Apple code-signing certificates imported into the Windows store carry the team ID as their OU.
    static int TeamIds()
    {
        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
        var teams = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var cert in store.Certificates)
        {
            var cn = cert.GetNameInfo(X509NameType.SimpleName, false) ?? "";
            if (!Regex.IsMatch(cn, @"^(Apple|iPhone|Mac) (Development|Distribution|Developer)")) continue;
            var ou = Regex.Match(cert.Subject, @"(?:^|,\s*)OU=([A-Z0-9]{10})(?:,|$)");
            if (!ou.Success) continue;
            var org = Regex.Match(cert.Subject, @"(?:^|,\s*)O=(""[^""]*""|[^,]*)");
            teams.TryAdd(ou.Groups[1].Value, org.Success ? org.Groups[1].Value.Trim('"') : cn);
        }
        if (teams.Count == 0)
        {
            Console.WriteLine("Certificates are not found");
            return 0;
        }
        foreach (var (id, name) in teams) Console.WriteLine($"{id} ({name})");
        return 0;
    }

    // Android Studio: the standard installer location, the per-user location and Toolbox installs,
    // plus the install path the installer records in the registry.
    static List<(string Home, string Version, string DataName)> FindAndroidStudios()
    {
        var candidates = new List<string>
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android", "Android Studio"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Android Studio"),
        };
        foreach (var root in new[] { Registry.LocalMachine, Registry.CurrentUser })
        {
            using var key = root.OpenSubKey(@"SOFTWARE\Android Studio");
            if (key?.GetValue("Path") is string p) candidates.Add(p);
        }
        var toolbox = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs");
        if (Directory.Exists(toolbox))
            candidates.AddRange(Directory.GetDirectories(toolbox, "Android Studio*"));
        var found = new List<(string, string, string)>();
        foreach (var home in candidates.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var info = Path.Combine(home, "product-info.json");
            if (!File.Exists(info)) continue;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(info));
                var r = doc.RootElement;
                var version = r.TryGetProperty("version", out var v) ? v.GetString() : "unknown";
                if (r.TryGetProperty("versionSuffix", out var suffix) && suffix.GetString() is { Length: > 0 } sfx) version += " " + sfx;
                var dataName = r.TryGetProperty("dataDirectoryName", out var d) ? d.GetString() : null;
                found.Add((home, version, dataName));
            }
            catch (JsonException) { found.Add((home, "unknown", null)); }
        }
        return found;
    }

    // A plugin directory's version is in META-INF/plugin.xml inside one of its lib/*.jar files.
    static string ReadPluginVersion(string pluginDir)
    {
        var lib = Path.Combine(pluginDir, "lib");
        if (!Directory.Exists(lib)) return null;
        foreach (var jar in Directory.GetFiles(lib, "*.jar"))
        {
            try
            {
                using var zip = System.IO.Compression.ZipFile.OpenRead(jar);
                var entry = zip.GetEntry("META-INF/plugin.xml");
                if (entry == null) continue;
                using var reader = new StreamReader(entry.Open());
                var m = Regex.Match(reader.ReadToEnd(), @"<version>([^<]+)</version>");
                return m.Success ? m.Groups[1].Value.Trim() : "installed";
            }
            catch (InvalidDataException) { }
            catch (IOException) { }
        }
        return Directory.Exists(pluginDir) ? "installed" : null;
    }

    static string JavaVersion(string java)
    {
        try
        {
            var psi = new ProcessStartInfo(java, "-version") { RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi);
            var err = p.StandardError.ReadToEnd();
            p.StandardOutput.ReadToEnd();
            p.WaitForExit(15000);
            return err.Split('\n').FirstOrDefault()?.Trim();
        }
        catch (System.ComponentModel.Win32Exception) { return null; }
    }

    static string FindOnPath(string exe)
    {
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(dir.Trim('"'), exe);
                if (File.Exists(candidate)) return candidate;
            }
            catch (ArgumentException) { }
        }
        return null;
    }

    static bool SamePath(string a, string b) =>
        string.Equals(Path.GetFullPath(a).TrimEnd('\\'), Path.GetFullPath(b).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
}
