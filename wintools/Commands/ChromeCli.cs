using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace WinTools;

// chrome-cli: control Google Chrome (or Edge, Brave, any Chromium) from the command line. The macOS
// tool uses AppleScript; this one uses the Chrome DevTools Protocol on Chrome's remote-debugging port
// (127.0.0.1:$CHROME_CLI_PORT, 9222 by default): the HTTP /json endpoints for tabs, and a WebSocket
// for scripting a page or asking the browser which window a tab is in. Tab ids are DevTools target
// ids; any unique prefix of one works.
static class ChromeCli
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("chrome-cli", "Control Google Chrome from the command-line (DevTools protocol)", Run);

    sealed record Tab(string Id, string Title, string Url, string Socket);

    static int Port => int.TryParse(Environment.GetEnvironmentVariable("CHROME_CLI_PORT"), out var p) ? p : 9222;

    static string Base => $"http://127.0.0.1:{Port}";

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine("""
                Usage: chrome-cli <command> [options]
                Talks to Chrome's DevTools port; start Chrome with
                  chrome --remote-debugging-port=9222 --user-data-dir=<profile folder>
                (set CHROME_CLI_PORT for another port). -t takes a tab id (or a unique prefix of one).
                  list windows                     List all windows
                  list tabs [-w <id>]              List all tabs, or the tabs in one window
                  list links [-w <id>]             List all tabs' urls
                  info [-t <id>]                   Print info for the active tab, or for a specific tab
                  open <url> [-n] [-i]             Open url in a new tab (-n: new window, -i: incognito window)
                  open <url> -t <id>               Open url in a specific tab
                  close [-t <id> | -w <id>]        Close the active tab, a specific tab or a window
                  reload [-t <id>]                 Reload the active tab or a specific tab
                  back|forward [-t <id>]           Navigate back/forward
                  activate -t <id>                 Activate a specific tab
                  source [-t <id>]                 Print the page source
                  execute <javascript> [-t <id>]   Run JavaScript in the page and print the result
                  version                          Print Chrome's version
                """);
            return 0;
        }
        var cmd = args[0];
        string tabId = null, windowId = null;
        bool newWindow = false, incognito = false;
        var positional = new List<string>();
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-t": tabId = ++i < args.Length ? args[i] : throw new UsageException("-t needs a tab id"); break;
                case "-w": windowId = ++i < args.Length ? args[i] : throw new UsageException("-w needs a window id"); break;
                case "-n": newWindow = true; break;
                case "-i": incognito = true; break;
                default: positional.Add(args[i]); break;
            }
        }
        switch (cmd)
        {
            case "list":
                return positional.FirstOrDefault() switch
                {
                    "windows" => ListWindows(),
                    "tabs" => ListTabs(windowId, links: false),
                    "links" => ListTabs(windowId, links: true),
                    _ => throw new UsageException("usage: chrome-cli list windows|tabs|links"),
                };
            case "info": return Info(FindTab(tabId));
            case "open":
                if (positional.Count != 1) throw new UsageException("usage: chrome-cli open <url> [-n] [-i] [-t <id>]");
                return Open(positional[0], tabId, newWindow, incognito);
            case "close": return Close(tabId, windowId);
            case "reload": Call(FindTab(tabId), "Page.reload", w => w.WriteBoolean("ignoreCache", false)).Dispose(); return 0;
            case "back": Evaluate(FindTab(tabId), "history.back()"); return 0;
            case "forward": Evaluate(FindTab(tabId), "history.forward()"); return 0;
            case "activate":
                if (tabId == null) throw new UsageException("usage: chrome-cli activate -t <id>");
                Get("/json/activate/" + FindTab(tabId).Id);
                return 0;
            case "source": Console.WriteLine(Evaluate(FindTab(tabId), "document.documentElement.outerHTML")); return 0;
            case "execute":
                if (positional.Count != 1) throw new UsageException("usage: chrome-cli execute <javascript> [-t <id>]");
                Console.WriteLine(Evaluate(FindTab(tabId), positional[0]));
                return 0;
            case "version":
                using (var doc = JsonDocument.Parse(Get("/json/version")))
                    Console.WriteLine(Text(doc.RootElement, "Browser").Replace('/', ' '));
                return 0;
            default: throw new UsageException($"unknown command '{cmd}' (see chrome-cli --help)");
        }
    }

    // --- Commands -----------------------------------------------------------------------------------

    static int ListWindows()
    {
        var tabs = Tabs();
        var windows = Windows(tabs);
        foreach (var group in tabs.GroupBy(t => windows[t.Id]))
            Console.WriteLine($"[{group.Key}] {group.First().Title}");
        return 0;
    }

    static int ListTabs(string windowId, bool links)
    {
        var tabs = Tabs();
        var windows = Windows(tabs);
        var multiple = windows.Values.Distinct().Count() > 1;
        foreach (var tab in tabs)
        {
            var window = windows[tab.Id];
            if (windowId != null && window.ToString() != windowId) continue;
            var id = multiple && windowId == null ? $"{window}:{tab.Id}" : tab.Id;
            Console.WriteLine($"[{id}] {(links ? tab.Url : tab.Title)}");
        }
        return 0;
    }

    static int Info(Tab tab)
    {
        Console.WriteLine($"Id: {tab.Id}");
        Console.WriteLine($"Window id: {Windows([tab])[tab.Id]}");
        Console.WriteLine($"Title: {tab.Title}");
        Console.WriteLine($"Url: {tab.Url}");
        string state;
        try { state = Evaluate(tab, "document.readyState"); }
        catch (InvalidOperationException) { state = "complete"; }
        Console.WriteLine($"Loading: {(state == "complete" ? "No" : "Yes")}");
        return 0;
    }

    static int Open(string url, string tabId, bool newWindow, bool incognito)
    {
        if (!url.Contains("://") && !url.StartsWith("about:")) url = "https://" + url;
        if (tabId != null)
        {
            var tab = FindTab(tabId);
            Call(tab, "Page.navigate", w => w.WriteString("url", url)).Dispose();
            return Info(tab with { Url = url });
        }
        string id;
        if (newWindow || incognito)
        {
            using var browser = Browser();
            string context = null;
            if (incognito)
                using (var ctx = Send(browser, "Target.createBrowserContext", _ => { }))
                    context = Text(ctx.RootElement, "browserContextId");
            using var created = Send(browser, "Target.createTarget", w =>
            {
                w.WriteString("url", url);
                w.WriteBoolean("newWindow", true);
                if (context != null) w.WriteString("browserContextId", context);
            });
            id = Text(created.RootElement, "targetId");
        }
        else
        {
            // Chrome 111+ only accepts PUT here; older ones take GET.
            string body;
            try { body = Request(HttpMethod.Put, "/json/new?" + url); }
            catch (InvalidOperationException) { body = Get("/json/new?" + url); }
            using var doc = JsonDocument.Parse(body);
            id = Text(doc.RootElement, "id");
        }
        var opened = Tabs().FirstOrDefault(t => t.Id == id);
        if (opened != null) Info(opened);
        return 0;
    }

    static int Close(string tabId, string windowId)
    {
        if (windowId != null)
        {
            var all = Tabs();
            var windows = Windows(all);
            var tabs = all.Where(t => windows[t.Id].ToString() == windowId).ToList();
            if (tabs.Count == 0) throw new InvalidOperationException($"no window with id {windowId}");
            foreach (var tab in tabs) Get("/json/close/" + tab.Id);
            return 0;
        }
        Get("/json/close/" + FindTab(tabId).Id);
        return 0;
    }

    // --- DevTools plumbing ----------------------------------------------------------------------------

    static List<Tab> Tabs()
    {
        using var doc = JsonDocument.Parse(Get("/json/list"));
        var tabs = new List<Tab>();
        foreach (var target in doc.RootElement.EnumerateArray())
            if (Text(target, "type") == "page")
                tabs.Add(new Tab(Text(target, "id"), Text(target, "title"), Text(target, "url"), Text(target, "webSocketDebuggerUrl")));
        return tabs;
    }

    // No -t: the active tab, which DevTools lists first.
    static Tab FindTab(string id)
    {
        var tabs = Tabs();
        if (id == null) return tabs.FirstOrDefault() ?? throw new InvalidOperationException("Chrome has no open tabs");
        if (id.Contains(':')) id = id[(id.IndexOf(':') + 1)..];   // "window:tab" as printed by list tabs
        var matches = tabs.Where(t => t.Id.StartsWith(id, StringComparison.OrdinalIgnoreCase)).ToList();
        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException($"no tab with id {id}"),
            _ => matches.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                 ?? throw new InvalidOperationException($"tab id {id} is ambiguous"),
        };
    }

    // Window id of each tab, asked of the browser over one connection (0 when the browser endpoint
    // isn't available, e.g. a page-only DevTools server).
    static Dictionary<string, long> Windows(List<Tab> tabs)
    {
        var windows = new Dictionary<string, long>();
        foreach (var tab in tabs) windows[tab.Id] = 0;
        ClientWebSocket browser;
        try { browser = Browser(); }
        catch (Exception e) when (e is InvalidOperationException or JsonException or UriFormatException) { return windows; }
        using (browser)
            foreach (var tab in tabs)
            {
                try
                {
                    using var result = Send(browser, "Browser.getWindowForTarget", w => w.WriteString("targetId", tab.Id));
                    if (result.RootElement.TryGetProperty("windowId", out var id)) windows[tab.Id] = id.GetInt64();
                }
                catch (InvalidOperationException) { }
            }
        return windows;
    }

    static string Evaluate(Tab tab, string expression)
    {
        using var result = Call(tab, "Runtime.evaluate", w =>
        {
            w.WriteString("expression", expression);
            w.WriteBoolean("returnByValue", true);
            w.WriteBoolean("awaitPromise", true);
        });
        var root = result.RootElement;
        if (root.TryGetProperty("exceptionDetails", out var exception))
        {
            var text = exception.TryGetProperty("exception", out var ex) && ex.TryGetProperty("description", out var d)
                ? d.GetString() : Text(exception, "text");
            throw new InvalidOperationException(text);
        }
        if (!root.TryGetProperty("result", out var value) || !value.TryGetProperty("value", out var v)) return "";
        return v.ValueKind == JsonValueKind.String ? v.GetString() : v.GetRawText();
    }

    static JsonDocument Call(Tab tab, string method, Action<Utf8JsonWriter> parameters)
    {
        if (string.IsNullOrEmpty(tab.Socket))
            throw new InvalidOperationException($"tab {tab.Id} is already being debugged by another DevTools client");
        using var socket = Connect(tab.Socket);
        return Send(socket, method, parameters);
    }

    static ClientWebSocket Browser()
    {
        using var doc = JsonDocument.Parse(Get("/json/version"));
        return Connect(Text(doc.RootElement, "webSocketDebuggerUrl"));
    }

    static ClientWebSocket Connect(string url)
    {
        var socket = new ClientWebSocket();
        try { socket.ConnectAsync(new Uri(url), CancellationToken.None).GetAwaiter().GetResult(); }
        catch (WebSocketException e) { socket.Dispose(); throw new InvalidOperationException($"can't open {url}: {e.Message}"); }
        return socket;
    }

    // Sends one command and waits for the reply with the same id (events in between are skipped).
    static JsonDocument Send(ClientWebSocket socket, string method, Action<Utf8JsonWriter> parameters)
    {
        try { return SendOnce(socket, method, parameters); }
        catch (Exception e) when (e is WebSocketException or OperationCanceledException)
        {
            throw new InvalidOperationException($"{method}: no answer from Chrome ({e.Message})");
        }
    }

    static JsonDocument SendOnce(ClientWebSocket socket, string method, Action<Utf8JsonWriter> parameters)
    {
        var message = new MemoryStream();
        using (var w = new Utf8JsonWriter(message))
        {
            w.WriteStartObject();
            w.WriteNumber("id", 1);
            w.WriteString("method", method);
            w.WriteStartObject("params");
            parameters(w);
            w.WriteEndObject();
            w.WriteEndObject();
        }
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        socket.SendAsync(message.ToArray(), WebSocketMessageType.Text, true, timeout.Token).GetAwaiter().GetResult();
        var buffer = new byte[64 * 1024];
        while (true)
        {
            var reply = new MemoryStream();
            WebSocketReceiveResult received;
            do
            {
                received = socket.ReceiveAsync(buffer, timeout.Token).GetAwaiter().GetResult();
                if (received.MessageType == WebSocketMessageType.Close) throw new InvalidOperationException("Chrome closed the DevTools connection");
                reply.Write(buffer, 0, received.Count);
            } while (!received.EndOfMessage);
            var doc = JsonDocument.Parse(reply.ToArray());
            var root = doc.RootElement;
            if (!root.TryGetProperty("id", out var id) || id.GetInt32() != 1) { doc.Dispose(); continue; }
            if (root.TryGetProperty("error", out var error))
            {
                var text = Text(error, "message");
                doc.Dispose();
                throw new InvalidOperationException($"{method}: {text}");
            }
            var result = JsonDocument.Parse(root.GetProperty("result").GetRawText());
            doc.Dispose();
            return result;
        }
    }

    static string Get(string path) => Request(HttpMethod.Get, path);

    static string Request(HttpMethod method, string path)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        try
        {
            using var response = http.Send(new HttpRequestMessage(method, Base + path));
            var body = new StreamReader(response.Content.ReadAsStream()).ReadToEnd();
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Chrome answered {(int)response.StatusCode}: {body.Trim()}");
            return body;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            throw new InvalidOperationException(
                $"can't reach Chrome's DevTools port 127.0.0.1:{Port}; start Chrome with --remote-debugging-port={Port} --user-data-dir=<profile folder>");
        }
    }

    static string Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : "";
}
