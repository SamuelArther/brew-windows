using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// choose (choose-gui): reads lines from stdin, shows a small always-on-top window with a search
// box and a fuzzy-filtered list (Win32 edit + owner-drawn list box), and prints the chosen line.
// Up/Down (or Ctrl+N/P) move, Enter picks, Esc cancels with status 1.
static class Choose
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("choose", "Fuzzy-pick a line from stdin in a small native window", Run);
    }

    const string Usage = """
        Usage: choose [options] < lines
          -i          return the index of the selected element
          -v          print the version
          -n [10]     number of rows to show
          -w [50]     width of the window, as a percentage of the screen
          -f [font]   font name (default Segoe UI)
          -s [size]   font size (default 12)
          -c [hex]    highlight colour, e.g. 0000FF
          -b [hex]    background colour
          -u          don't underline matched letters (accepted; matches are not underlined)
          -m          return the query string when nothing matches
          -a          rank early matches higher
          -z          accepted for compatibility
          -p [text]   prompt shown in the window title
          -o [query]  print the matches for query to stdout instead of showing a window
          -h          show this help
        """;

    static List<string> items = [];
    static List<int> shown = [];
    static bool earlyBonus, returnQuery;
    static uint highlight = 0xD77800, background = 0xFFFFFF, foreground = 0x000000;
    static IntPtr window, edit, list, font;
    static int result = -1;
    static string query = "";

    static int Run(string[] args)
    {
        bool index = false;
        int rows = 10, width = 50, size = 12;
        string fontName = "Segoe UI", prompt = "choose", batch = null;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"option {a} needs a value");
            switch (a)
            {
                case "-h" or "--help": Console.WriteLine(Usage); return 0;
                case "-v" or "--version": Console.WriteLine($"choose {Commands.Version} (wintools)"); return 0;
                case "-i": index = true; break;
                case "-n": rows = Math.Clamp(Int(Value()), 1, 50); break;
                case "-w": width = Math.Clamp(Int(Value()), 10, 100); break;
                case "-f": fontName = Value(); break;
                case "-s": size = Math.Clamp(Int(Value()), 6, 72); break;
                case "-c": highlight = Bgr(Value()); break;
                case "-b": background = Bgr(Value()); break;
                case "-u" or "-z": break;
                case "-m": returnQuery = true; break;
                case "-a": earlyBonus = true; break;
                case "-p": prompt = Value(); break;
                case "-o": batch = Value(); break;
                default: throw new UsageException($"unknown option {a}\n{Usage}");
            }
        }
        string line;
        while ((line = Console.In.ReadLine()) != null) if (line.Length > 0) items.Add(line);
        if (batch != null)
        {
            foreach (var k in Filter(batch)) Console.WriteLine(index ? k.ToString() : items[k]);
            return 0;
        }
        if (items.Count == 0 && !returnQuery) return 1;
        // Light text on a dark background stays readable.
        var luminance = (background & 0xFF) * 0.0722 + ((background >> 8) & 0xFF) * 0.7152 + ((background >> 16) & 0xFF) * 0.2126;
        foreground = luminance < 128 ? 0xFFFFFFu : 0;
        ShowWindow(prompt, rows, width, fontName, size);
        if (result >= 0) { Console.WriteLine(index ? result.ToString() : items[result]); return 0; }
        if (result == -2) { Console.WriteLine(query); return 0; }
        return 1;
    }

    static int Int(string s) => int.TryParse(s, out var n) ? n : throw new UsageException($"not a number: {s}");

    static uint Bgr(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6 || !uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var rgb)) throw new UsageException($"bad colour '{hex}' (use RRGGBB)");
        return ((rgb & 0xFF) << 16) | (rgb & 0xFF00) | ((rgb >> 16) & 0xFF);
    }

    // Subsequence match, case-insensitive. Consecutive letters and word starts score higher; with
    // -a an earlier first match does too. Ties keep input order.
    internal static List<int> Filter(string q)
    {
        if (q.Length == 0) return Enumerable.Range(0, items.Count).ToList();
        var scored = new List<(int Index, int Score)>();
        for (var k = 0; k < items.Count; k++)
        {
            var s = Score(items[k], q);
            if (s >= 0) scored.Add((k, s));
        }
        return scored.OrderByDescending(x => x.Score).ThenBy(x => x.Index).Select(x => x.Index).ToList();
    }

    static int Score(string item, string q)
    {
        int score = 0, pos = 0, last = -2, first = -1;
        foreach (var c in q)
        {
            var found = -1;
            for (var j = pos; j < item.Length; j++)
                if (char.ToLowerInvariant(item[j]) == char.ToLowerInvariant(c)) { found = j; break; }
            if (found < 0) return -1;
            if (first < 0) first = found;
            score += 1;
            if (found == last + 1) score += 5;
            if (found == 0 || !char.IsLetterOrDigit(item[found - 1]) || char.IsUpper(item[found]) && char.IsLower(item[found - 1])) score += 3;
            last = found;
            pos = found + 1;
        }
        if (earlyBonus) score += Math.Max(0, 20 - first);
        return score - item.Length / 16;
    }

    static unsafe void ShowWindow(string title, int rows, int widthPercent, string fontName, int size)
    {
        var instance = GetModuleHandleW(null);
        var className = "wintools-choose";
        fixed (char* cls = className)
        {
            var wc = new WNDCLASSEXW
            {
                cbSize = (uint)sizeof(WNDCLASSEXW),
                lpfnWndProc = (IntPtr)(delegate* unmanaged<IntPtr, uint, IntPtr, IntPtr, IntPtr>)&WndProc,
                hInstance = instance,
                hCursor = LoadCursorW(IntPtr.Zero, (IntPtr)32512),
                hbrBackground = CreateSolidBrush(background),
                lpszClassName = cls,
            };
            RegisterClassExW(ref wc);
        }
        var screen = GetDC(IntPtr.Zero);
        var dpi = GetDeviceCaps(screen, LOGPIXELSY);
        ReleaseDC(IntPtr.Zero, screen);
        var fontHeight = size * dpi / 72;
        font = CreateFontW(-fontHeight, 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 5, 0, fontName);
        var itemHeight = fontHeight * 3 / 2;
        var pad = itemHeight / 3;
        int screenW = GetSystemMetrics(0), screenH = GetSystemMetrics(1);
        var w = screenW * widthPercent / 100;
        var h = pad * 3 + itemHeight + rows * itemHeight + 2;
        window = CreateWindowExW(WS_EX_TOPMOST | WS_EX_TOOLWINDOW, className, title, WS_POPUP | WS_BORDER,
            (screenW - w) / 2, (screenH - h) / 3, w, h, IntPtr.Zero, IntPtr.Zero, instance, IntPtr.Zero);
        edit = CreateWindowExW(0, "EDIT", "", WS_CHILD | WS_VISIBLE | ES_AUTOHSCROLL,
            pad, pad, w - 2 * pad, itemHeight, window, (IntPtr)1, instance, IntPtr.Zero);
        list = CreateWindowExW(0, "LISTBOX", "", WS_CHILD | WS_VISIBLE | LBS_OWNERDRAWFIXED | LBS_NOTIFY | LBS_NOINTEGRALHEIGHT | WS_VSCROLL,
            pad, pad * 2 + itemHeight, w - 2 * pad, rows * itemHeight, window, (IntPtr)2, instance, IntPtr.Zero);
        SendMessageW(edit, WM_SETFONT, font, 1);
        SendMessageW(list, LB_SETITEMHEIGHT, IntPtr.Zero, itemHeight);
        Refilter();
        ShowWindowNative(window, SW_SHOW);
        SetForegroundWindow(window);
        SetFocus(edit);
        while (GetMessageW(out var msg, IntPtr.Zero, 0, 0) > 0)
        {
            if (msg.message == WM_KEYDOWN && HandleKey((int)msg.wParam)) continue;
            if (msg.message == WM_CHAR && msg.wParam is 0x0E or 0x10) continue;     // Ctrl+N / Ctrl+P already handled
            TranslateMessage(ref msg);
            DispatchMessageW(ref msg);
        }
        DeleteObject(font);
    }

    static bool HandleKey(int key)
    {
        var ctrl = GetKeyState(VK_CONTROL) < 0;
        var count = shown.Count;
        var current = (int)SendMessageW(list, LB_GETCURSEL, IntPtr.Zero, IntPtr.Zero);
        switch (key)
        {
            case VK_DOWN or VK_TAB:
                if (count > 0) SendMessageW(list, LB_SETCURSEL, Math.Min(count - 1, current + 1), IntPtr.Zero);
                return true;
            case VK_UP:
                if (count > 0) SendMessageW(list, LB_SETCURSEL, Math.Max(0, current - 1), IntPtr.Zero);
                return true;
            case 'N' when ctrl:
                return HandleKey(VK_DOWN);
            case 'P' when ctrl:
                return HandleKey(VK_UP);
            case VK_RETURN:
                Accept();
                return true;
            case VK_ESCAPE:
                result = -1;
                DestroyWindow(window);
                return true;
        }
        return false;
    }

    static void Accept()
    {
        var current = (int)SendMessageW(list, LB_GETCURSEL, IntPtr.Zero, IntPtr.Zero);
        if (current >= 0 && current < shown.Count) result = shown[current];
        else if (returnQuery) result = -2;
        else return;
        DestroyWindow(window);
    }

    static unsafe void Refilter()
    {
        var length = (int)SendMessageW(edit, WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);
        var buffer = new char[length + 1];
        fixed (char* p = buffer) SendMessageW(edit, WM_GETTEXT, length + 1, (IntPtr)p);
        query = new string(buffer, 0, length);
        shown = Filter(query);
        SendMessageW(list, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        SendMessageW(list, LB_RESETCONTENT, IntPtr.Zero, IntPtr.Zero);
        foreach (var k in shown) SendMessageW(list, LB_ADDSTRING, IntPtr.Zero, k);   // item data: the input index
        if (shown.Count > 0) SendMessageW(list, LB_SETCURSEL, IntPtr.Zero, IntPtr.Zero);
        SendMessageW(list, WM_SETREDRAW, 1, IntPtr.Zero);
        InvalidateRect(list, IntPtr.Zero, 1);
    }

    [UnmanagedCallersOnly]
    static unsafe IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WM_COMMAND:
                var code = (int)((long)wParam >> 16) & 0xFFFF;
                if (lParam == edit && code == EN_CHANGE) Refilter();
                else if (lParam == list && code == LBN_DBLCLK) Accept();
                return IntPtr.Zero;
            case WM_DRAWITEM:
                var item = (DRAWITEMSTRUCT*)lParam;
                if (item->itemID < 0 || item->itemID >= shown.Count) return 1;
                var selected = (item->itemState & ODS_SELECTED) != 0;
                var brush = CreateSolidBrush(selected ? highlight : background);
                FillRect(item->hDC, &item->rcItem, brush);
                DeleteObject(brush);
                SetBkMode(item->hDC, 1);
                SetTextColor(item->hDC, selected ? 0xFFFFFFu : foreground);
                var old = SelectObject(item->hDC, font);
                var rect = item->rcItem;
                rect.left += 6;
                var text = items[shown[item->itemID]];
                fixed (char* t = text) DrawTextW(item->hDC, t, text.Length, &rect, DT_SINGLELINE | DT_VCENTER | DT_NOPREFIX | DT_END_ELLIPSIS);
                SelectObject(item->hDC, old);
                return 1;
            case WM_CTLCOLOREDIT or WM_CTLCOLORLISTBOX:
                SetBkColor(wParam, background);
                SetTextColor(wParam, foreground);
                return GetBackgroundBrush();
            case WM_ACTIVATE:
                // Clicking elsewhere cancels, like the macOS original.
                if (((long)wParam & 0xFFFF) == 0 && window != IntPtr.Zero && IsWindow(window)) DestroyWindow(hwnd);
                return IntPtr.Zero;
            case WM_DESTROY:
                PostQuitMessage(0);
                return IntPtr.Zero;
        }
        return DefWindowProcW(hwnd, msg, wParam, lParam);
    }

    static IntPtr backgroundBrush;
    static IntPtr GetBackgroundBrush() => backgroundBrush != IntPtr.Zero ? backgroundBrush : backgroundBrush = CreateSolidBrush(background);

    const uint WS_POPUP = 0x80000000, WS_BORDER = 0x00800000, WS_CHILD = 0x40000000, WS_VISIBLE = 0x10000000, WS_VSCROLL = 0x00200000;
    const uint WS_EX_TOPMOST = 0x8, WS_EX_TOOLWINDOW = 0x80, ES_AUTOHSCROLL = 0x80;
    const uint LBS_NOTIFY = 0x1, LBS_OWNERDRAWFIXED = 0x10, LBS_NOINTEGRALHEIGHT = 0x100;
    const uint WM_DESTROY = 0x2, WM_ACTIVATE = 0x6, WM_SETREDRAW = 0xB, WM_GETTEXT = 0xD, WM_GETTEXTLENGTH = 0xE, WM_SETFONT = 0x30, WM_DRAWITEM = 0x2B;
    const uint WM_KEYDOWN = 0x100, WM_CHAR = 0x102, WM_COMMAND = 0x111, WM_CTLCOLOREDIT = 0x133, WM_CTLCOLORLISTBOX = 0x134;
    const uint LB_ADDSTRING = 0x180, LB_RESETCONTENT = 0x184, LB_SETCURSEL = 0x186, LB_GETCURSEL = 0x188, LB_SETITEMHEIGHT = 0x1A0;
    const int EN_CHANGE = 0x300, LBN_DBLCLK = 2, ODS_SELECTED = 1, SW_SHOW = 5, LOGPIXELSY = 90;
    const int VK_TAB = 0x09, VK_RETURN = 0x0D, VK_CONTROL = 0x11, VK_ESCAPE = 0x1B, VK_UP = 0x26, VK_DOWN = 0x28;
    const uint DT_VCENTER = 0x4, DT_SINGLELINE = 0x20, DT_NOPREFIX = 0x800, DT_END_ELLIPSIS = 0x8000;

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct WNDCLASSEXW
    {
        public uint cbSize, style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra, cbWndExtra;
        public IntPtr hInstance, hIcon, hCursor, hbrBackground;
        public char* lpszMenuName, lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MSG { public IntPtr hwnd; public uint message; public IntPtr wParam, lParam; public uint time; public int x, y; }

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int left, top, right, bottom; }

    [StructLayout(LayoutKind.Sequential)]
    struct DRAWITEMSTRUCT
    {
        public uint CtlType, CtlID;
        public int itemID;
        public uint itemAction, itemState;
        public IntPtr hwndItem, hDC;
        public RECT rcItem;
        public IntPtr itemData;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandleW(string name);
    [DllImport("user32.dll")] static extern ushort RegisterClassExW(ref WNDCLASSEXW wc);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr CreateWindowExW(uint exStyle, string cls, string name, uint style, int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);
    [DllImport("user32.dll", EntryPoint = "ShowWindow")] static extern bool ShowWindowNative(IntPtr hwnd, int cmd);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] static extern IntPtr SetFocus(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool DestroyWindow(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll")] static extern IntPtr SendMessageW(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] static extern int GetMessageW(out MSG msg, IntPtr hwnd, uint min, uint max);
    [DllImport("user32.dll")] static extern bool TranslateMessage(ref MSG msg);
    [DllImport("user32.dll")] static extern IntPtr DispatchMessageW(ref MSG msg);
    [DllImport("user32.dll")] static extern void PostQuitMessage(int code);
    [DllImport("user32.dll")] static extern IntPtr DefWindowProcW(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] static extern short GetKeyState(int key);
    [DllImport("user32.dll")] static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] static extern IntPtr LoadCursorW(IntPtr instance, IntPtr name);
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
    [DllImport("user32.dll")] static extern bool InvalidateRect(IntPtr hwnd, IntPtr rect, int erase);
    [DllImport("user32.dll")] static extern unsafe int FillRect(IntPtr dc, RECT* rect, IntPtr brush);
    [DllImport("user32.dll")] static extern unsafe int DrawTextW(IntPtr dc, char* text, int length, RECT* rect, uint format);
    [DllImport("gdi32.dll")] static extern int GetDeviceCaps(IntPtr dc, int index);
    [DllImport("gdi32.dll")] static extern IntPtr CreateSolidBrush(uint color);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll")] static extern int SetBkMode(IntPtr dc, int mode);
    [DllImport("gdi32.dll")] static extern uint SetBkColor(IntPtr dc, uint color);
    [DllImport("gdi32.dll")] static extern uint SetTextColor(IntPtr dc, uint color);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr CreateFontW(int height, int width, int escapement, int orientation, int weight, uint italic, uint underline, uint strikeOut,
        uint charSet, uint outPrecision, uint clipPrecision, uint quality, uint pitchAndFamily, string face);
}
