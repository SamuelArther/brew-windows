using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// lazycontainer: a terminal UI over the WSL2 `container` store. Containers and images on the left,
// details or logs of the selection on the right; keys start, stop, delete and inspect. Drawn with VT
// sequences on the alternate screen; when output isn't a console it prints one snapshot and exits.
static class Lazycontainer
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("lazycontainer", "Terminal UI for WSL2 containers and images", Run);
    }

    const string Help = """
        Usage: lazycontainer [--version]
        Terminal UI for containers and images (the container command's WSL2 store).

        Keys:
          Tab / Left / Right   switch between the Containers and Images panels
          Up / Down / j / k    move the selection
          Enter / i            inspect the selection        l   show the container's logs
          s                    start the container          x   stop the container
          d                    delete the container or image
          r                    refresh                      q / Esc   quit
        """;

    static int Run(string[] args)
    {
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help": Console.WriteLine(Help); return 0;
                case "-v" or "--version" or "version": Console.WriteLine($"lazycontainer version {Commands.Version}"); return 0;
                default: throw new UsageException($"unknown option '{arg}'");
            }
        }
        if (Console.IsOutputRedirected || Console.IsInputRedirected) return Snapshot();
        return new Ui().Loop();
    }

    // Non-interactive: print both lists once.
    static int Snapshot()
    {
        List<Container.Box> boxes;
        List<Container.ImageInfo> images;
        try { boxes = Container.ListBoxes(); images = Container.ListImages(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            Console.Error.WriteLine($"Error listing containers: {e.Message}");
            return 1;
        }
        Console.WriteLine($"Containers ({boxes.Count})");
        foreach (var b in boxes) Console.WriteLine($"  {Container.State(b),-8} {b.Id}  {b.Image}");
        Console.WriteLine($"Images ({images.Count})");
        foreach (var i in images) Console.WriteLine($"  {i.Reference}  {i.Digest[7..19]}  {Container.FormatSize(i.Size)}");
        return 0;
    }

    sealed class Ui
    {
        List<Container.Box> boxes = [];
        List<Container.ImageInfo> images = [];
        int panel, boxIndex, imageIndex;
        bool showLogs;
        string status = "";
        uint oldMode;

        public int Loop()
        {
            var handle = GetStdHandle(-11);
            GetConsoleMode(handle, out oldMode);
            SetConsoleMode(handle, oldMode | 0x4 | 0x1); // ENABLE_VIRTUAL_TERMINAL_PROCESSING | ENABLE_PROCESSED_OUTPUT
            var oldEncoding = Console.OutputEncoding;
            Console.OutputEncoding = Encoding.UTF8;
            Console.Write("\x1b[?1049h\x1b[?25l");
            Console.TreatControlCAsInput = true;
            try
            {
                Refresh();
                var lastRefresh = DateTime.UtcNow;
                Draw();
                while (true)
                {
                    if (!Console.KeyAvailable)
                    {
                        Thread.Sleep(50);
                        if ((DateTime.UtcNow - lastRefresh).TotalSeconds >= 2) { Refresh(); Draw(); lastRefresh = DateTime.UtcNow; }
                        continue;
                    }
                    var key = Console.ReadKey(true);
                    if (key.Key is ConsoleKey.Q or ConsoleKey.Escape || (key.Key == ConsoleKey.C && key.Modifiers.HasFlag(ConsoleModifiers.Control))) return 0;
                    Handle(key);
                    Draw();
                }
            }
            finally
            {
                Console.Write("\x1b[0m\x1b[?25h\x1b[?1049l");
                SetConsoleMode(handle, oldMode);
                Console.OutputEncoding = oldEncoding;
            }
        }

        void Refresh()
        {
            try
            {
                boxes = Container.ListBoxes();
                images = Container.ListImages();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
            {
                status = "Error listing containers: " + e.Message;
            }
            boxIndex = Math.Clamp(boxIndex, 0, Math.Max(0, boxes.Count - 1));
            imageIndex = Math.Clamp(imageIndex, 0, Math.Max(0, images.Count - 1));
        }

        Container.Box SelectedBox => panel == 0 && boxes.Count > 0 ? boxes[boxIndex] : null;
        Container.ImageInfo SelectedImage => panel == 1 && images.Count > 0 ? images[imageIndex] : null;

        void Handle(ConsoleKeyInfo key)
        {
            status = "";
            switch (key.Key)
            {
                case ConsoleKey.Tab or ConsoleKey.LeftArrow or ConsoleKey.RightArrow: panel = 1 - panel; showLogs = false; return;
                case ConsoleKey.UpArrow or ConsoleKey.K: Move(-1); return;
                case ConsoleKey.DownArrow or ConsoleKey.J: Move(1); return;
                case ConsoleKey.Enter or ConsoleKey.I: showLogs = false; return;
                case ConsoleKey.L: showLogs = SelectedBox != null; return;
                case ConsoleKey.R: Refresh(); status = "Refreshed"; return;
            }
            Act(key.Key);
            Refresh();
        }

        void Move(int delta)
        {
            if (panel == 0) boxIndex = Math.Clamp(boxIndex + delta, 0, Math.Max(0, boxes.Count - 1));
            else imageIndex = Math.Clamp(imageIndex + delta, 0, Math.Max(0, images.Count - 1));
        }

        // Actions run in-process through the container command; its own output is captured for the status line.
        void Act(ConsoleKey key)
        {
            string[] command = (key, SelectedBox, SelectedImage) switch
            {
                (ConsoleKey.S, { } b, _) => ["start", b.Id],
                (ConsoleKey.X, { } b, _) => ["stop", b.Id],
                (ConsoleKey.D, { } b, _) => ["delete", "--force", b.Id],
                (ConsoleKey.D, _, { } i) => ["image", "delete", i.Reference],
                _ => null,
            };
            if (command == null) return;
            status = $"container {string.Join(' ', command)} ...";
            Draw();
            var output = new StringWriter();
            var error = new StringWriter();
            var (oldOut, oldErr) = (Console.Out, Console.Error);
            Console.SetOut(output);
            Console.SetError(error);
            int code;
            try { code = Commands.Run("container", command); }
            finally { Console.SetOut(oldOut); Console.SetError(oldErr); }
            var message = (error.ToString() + output.ToString()).Trim().Split('\n').LastOrDefault()?.Trim() ?? "";
            status = code == 0 ? $"{command[0]} {command[^1]}: done" : message;
        }

        void Draw()
        {
            var width = Math.Max(40, Console.WindowWidth);
            var height = Math.Max(10, Console.WindowHeight);
            var left = Math.Min(60, width / 2);
            var right = width - left - 1;
            var leftLines = new List<string>();
            var boxRows = Math.Max(3, (height - 4) / 2);
            leftLines.Add(Title($" Containers ({boxes.Count}) ", panel == 0, left));
            leftLines.AddRange(Window(boxes.Select(b => $"{(Container.IsRunning(b) ? "\x1b[32m●\x1b[0m" : "\x1b[90m○\x1b[0m")} {b.Id}  {Short(b.Image)}").ToList(),
                boxIndex, panel == 0, boxRows - 1, left));
            leftLines.Add(Title($" Images ({images.Count}) ", panel == 1, left));
            leftLines.AddRange(Window(images.Select(i => $"{Short(i.Reference)}  {Container.FormatSize(i.Size)}").ToList(),
                imageIndex, panel == 1, height - 3 - boxRows - 1, left));

            var detail = Detail();
            var rightLines = new List<string> { Title(showLogs ? " Logs " : " Details ", true, right) };
            rightLines.AddRange(detail.SelectMany(l => Wrap(l, right)).TakeLast(showLogs ? height - 3 : int.MaxValue).Take(height - 3));

            var sb = new StringBuilder("\x1b[H");
            for (var row = 0; row < height - 1; row++)
            {
                var l = row < leftLines.Count ? leftLines[row] : "";
                var r = row < rightLines.Count ? rightLines[row] : "";
                sb.Append(Pad(l, left)).Append("\x1b[90m│\x1b[0m").Append(Pad(r, right)).Append("\x1b[K\r\n");
            }
            var help = status.Length > 0 ? status : "s start  x stop  d delete  l logs  i inspect  tab switch  r refresh  q quit";
            sb.Append("\x1b[7m").Append(Pad(" " + help, width - 1)).Append("\x1b[0m");
            Console.Write(sb.ToString());
        }

        List<string> Detail()
        {
            if (SelectedBox is { } b)
            {
                if (showLogs)
                {
                    if (!File.Exists(b.LogFile)) return ["(no output yet)"];
                    using var stream = new FileStream(b.LogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    if (stream.Length > 64 * 1024) stream.Position = stream.Length - 64 * 1024;
                    return new StreamReader(stream).ReadToEnd().Replace("\r", "").Split('\n').ToList();
                }
                var lines = new List<string>
                {
                    $"ID:        {b.Id}", $"Image:     {b.Image}", $"State:     {Container.State(b)}", $"Created:   {b.Created}",
                    $"Distro:    {b.Distro}", $"Arch:      {b.Arch}", $"Command:   {string.Join(' ', b.Command)}", $"Workdir:   {b.WorkDir}",
                };
                if (b.User != null) lines.Add($"User:      {b.User}");
                if (b.Ports.Count > 0) lines.Add($"Ports:     {string.Join(", ", b.Ports)}");
                foreach (var v in b.Volumes) { var (s, t, ro) = Container.SplitVolume(v); lines.Add($"Volume:    {s} -> {t}{(ro ? " (ro)" : "")}"); }
                foreach (var l in b.Labels) lines.Add($"Label:     {l}");
                lines.Add("Env:");
                lines.AddRange(b.Env.Select(e => "  " + e));
                return lines;
            }
            if (SelectedImage is { } i)
            {
                var lines = new List<string> { $"Reference: {i.Reference}", $"Digest:    {i.Digest}", $"Size:      {Container.FormatSize(i.Size)}" };
                try
                {
                    var c = Container.ReadConfig(i);
                    lines.Add($"Platform:  {c.Os}/{c.Architecture}");
                    if (c.Created != null) lines.Add($"Created:   {c.Created}");
                    lines.Add($"Entrypoint: {string.Join(' ', c.Entrypoint)}");
                    lines.Add($"Cmd:       {string.Join(' ', c.Cmd)}");
                    if (!string.IsNullOrEmpty(c.WorkingDir)) lines.Add($"Workdir:   {c.WorkingDir}");
                    if (c.Ports.Count > 0) lines.Add($"Exposed:   {string.Join(", ", c.Ports)}");
                    lines.Add($"Layers:    {c.Layers.Count}");
                    lines.Add("Env:");
                    lines.AddRange(c.Env.Select(e => "  " + e));
                }
                catch (Exception e) when (e is IOException or System.Text.Json.JsonException or InvalidOperationException) { lines.Add("(config unavailable: " + e.Message + ")"); }
                return lines;
            }
            return [panel == 0 ? "No containers. Create one with: container run -d <image>" : "No images. Pull one with: container image pull <image>"];
        }

        static string Short(string reference) =>
            reference.StartsWith("docker.io/library/", StringComparison.Ordinal) ? reference[18..]
            : reference.StartsWith("docker.io/", StringComparison.Ordinal) ? reference[10..] : reference;

        static string Title(string text, bool active, int width) =>
            (active ? "\x1b[1;36m" : "\x1b[90m") + Pad("─" + text + new string('─', Math.Max(0, width)), width) + "\x1b[0m";

        static IEnumerable<string> Window(List<string> items, int selected, bool active, int rows, int width)
        {
            var first = Math.Max(0, Math.Min(selected - rows + 1, items.Count - rows));
            first = Math.Max(0, Math.Min(first, selected));
            for (var r = 0; r < rows; r++)
            {
                var index = first + r;
                if (index >= items.Count) { yield return ""; continue; }
                var line = " " + items[index];
                yield return index == selected && active ? "\x1b[7m" + Pad(line, width) + "\x1b[0m" : line;
            }
        }

        static IEnumerable<string> Wrap(string line, int width)
        {
            if (width <= 1) yield break;
            line = line.Replace("\t", "    ");
            for (var i = 0; i < line.Length; i += width - 1) yield return " " + line.Substring(i, Math.Min(width - 1, line.Length - i));
            if (line.Length == 0) yield return "";
        }

        // Pads or cuts to a visible width, ignoring VT escape sequences.
        static string Pad(string text, int width)
        {
            var sb = new StringBuilder();
            var visible = 0;
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '\x1b')
                {
                    var end = i + 1;
                    while (end < text.Length && !char.IsAsciiLetter(text[end])) end++;
                    sb.Append(text, i, Math.Min(end + 1, text.Length) - i);
                    i = end;
                    continue;
                }
                if (visible >= width) continue;
                sb.Append(char.IsControl(text[i]) ? ' ' : text[i]);
                visible++;
            }
            return sb.Append(' ', Math.Max(0, width - visible)).Append("\x1b[0m").ToString();
        }
    }

    [DllImport("kernel32.dll")]
    static extern IntPtr GetStdHandle(int handle);

    [DllImport("kernel32.dll")]
    static extern bool GetConsoleMode(IntPtr handle, out uint mode);

    [DllImport("kernel32.dll")]
    static extern bool SetConsoleMode(IntPtr handle, uint mode);
}
