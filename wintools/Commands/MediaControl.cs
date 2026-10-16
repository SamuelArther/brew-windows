using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Media.Control;

namespace WinTools;

// media-control / nowplaying-cli: read what's playing and send play/pause/next/seek to it through the
// WinRT GlobalSystemMediaTransportControlsSessionManager - the same sessions the Windows media flyout
// shows (Spotify, browsers, Media Player, ...). The current session is the one Windows considers active.
static class MediaControl
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("media-control", "Control and observe media playback from the command-line", a => WinRT(() => Run(a), "the media session service"));
        Commands.Add("nowplaying-cli", "Retrieve currently playing media and simulate media actions", a => WinRT(() => RunNowPlaying(a), "the media session service"));
    }

    // ---- shared ---------------------------------------------------------------------------------

    sealed class NowPlaying
    {
        public string App, Title, Artist, Album, AlbumArtist, Genre, PlaybackStatus, Repeat, ArtworkMime;
        public bool? Shuffle;
        public double? Duration, Elapsed, Rate;
        public int? TrackNumber, TrackCount;
        public DateTimeOffset? Timestamp;
        public byte[] Artwork;
        public bool Playing => PlaybackStatus == "Playing";
    }

    static GlobalSystemMediaTransportControlsSession Session()
    {
        try
        {
            var manager = GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask().GetAwaiter().GetResult();
            return manager.GetCurrentSession();
        }
        catch (Exception)
        {
            return null;   // no media service (server SKUs, service sessions)
        }
    }

    static NowPlaying Read(GlobalSystemMediaTransportControlsSession session, bool artwork)
    {
        if (session == null) return null;
        var info = new NowPlaying { App = session.SourceAppUserModelId };
        try
        {
            var props = session.TryGetMediaPropertiesAsync().AsTask().GetAwaiter().GetResult();
            info.Title = NullIfEmpty(props.Title);
            info.Artist = NullIfEmpty(props.Artist);
            info.Album = NullIfEmpty(props.AlbumTitle);
            info.AlbumArtist = NullIfEmpty(props.AlbumArtist);
            info.Genre = props.Genres.Count > 0 ? props.Genres[0] : null;
            if (props.TrackNumber > 0) info.TrackNumber = props.TrackNumber;
            if (props.AlbumTrackCount > 0) info.TrackCount = props.AlbumTrackCount;
            if (artwork && props.Thumbnail != null)
            {
                using var stream = props.Thumbnail.OpenReadAsync().AsTask().GetAwaiter().GetResult();
                info.ArtworkMime = string.IsNullOrEmpty(stream.ContentType) ? "image/png" : stream.ContentType;
                using var s = stream.AsStreamForRead();
                using var ms = new MemoryStream();
                s.CopyTo(ms);
                info.Artwork = ms.ToArray();
            }
        }
        catch (Exception e) when (e is System.Runtime.InteropServices.COMException or InvalidOperationException or IOException) { }
        var playback = session.GetPlaybackInfo();
        info.PlaybackStatus = playback.PlaybackStatus.ToString();
        info.Rate = playback.PlaybackRate;
        info.Shuffle = playback.IsShuffleActive;
        info.Repeat = playback.AutoRepeatMode?.ToString();
        var timeline = session.GetTimelineProperties();
        var duration = (timeline.EndTime - timeline.StartTime).TotalSeconds;
        if (duration > 0) info.Duration = duration;
        if (duration > 0 || timeline.Position > TimeSpan.Zero) info.Elapsed = (timeline.Position - timeline.StartTime).TotalSeconds;
        if (timeline.LastUpdatedTime.Year > 1601) info.Timestamp = timeline.LastUpdatedTime;
        return info;
    }

    static string NullIfEmpty(string s) => string.IsNullOrEmpty(s) ? null : s;

    // Elapsed time extrapolated to now while playing (sessions only report position on changes).
    static double? ElapsedNow(NowPlaying p)
    {
        if (p.Elapsed is not double elapsed) return null;
        if (!p.Playing || p.Timestamp is not DateTimeOffset at) return elapsed;
        var now = elapsed + (DateTimeOffset.Now - at).TotalSeconds * (p.Rate ?? 1);
        return p.Duration is double d ? Math.Min(now, d) : now;
    }

    // WinRT reports failures as COMException; show them as one-line errors like every other tool.
    internal static int WinRT(Func<int> run, string what)
    {
        try { return run(); }
        catch (System.Runtime.InteropServices.COMException e) { throw new InvalidOperationException($"{what} failed (0x{e.HResult:x8}: {e.Message.Trim()})"); }
    }

    internal static string JsonString(string s)
    {
        if (s == null) return "null";
        var sb = new StringBuilder("\"");
        foreach (var c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20) sb.Append($"\\u{(int)c:x4}");
                    else sb.Append(c);
                    break;
            }
        }
        return sb.Append('"').ToString();
    }

    static string Num(double? d) => d is double v ? v.ToString("0.######", CultureInfo.InvariantCulture) : "null";

    // Run a transport command against the current session.
    static int Send(string name, Func<GlobalSystemMediaTransportControlsSession, Windows.Foundation.IAsyncOperation<bool>> action, string tool)
    {
        var session = Session();
        if (session == null) { Console.Error.WriteLine($"{tool}: nothing is playing (no media session)"); return 1; }
        if (action(session).AsTask().GetAwaiter().GetResult()) return 0;
        Console.Error.WriteLine($"{tool}: {session.SourceAppUserModelId} did not accept '{name}'");
        return 1;
    }

    static long Ticks(double seconds) => (long)(seconds * TimeSpan.TicksPerSecond);

    // ---- media-control --------------------------------------------------------------------------

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine("""
                Usage: media-control <command> [options]

                Commands:
                  get [--now] [--micros] [--no-artwork]   Print the now-playing information as JSON ("null" if nothing)
                  stream [--no-diff] [--debounce=<ms>] [--micros] [--no-artwork]
                                                          Print JSON lines whenever it changes
                  play | pause | toggle-play-pause | stop
                  next-track | previous-track
                  seek <seconds>                          Jump to a position
                  shuffle <on|off> | repeat <off|one|all> | speed <rate>
                  version
                Reads and controls the media session Windows shows in its media flyout.
                """);
            return args.Length == 0 ? 2 : 0;
        }
        var options = args.Skip(1).ToList();
        var micros = options.Remove("--micros");
        var artwork = !options.Remove("--no-artwork");
        const string tool = "media-control";
        switch (args[0])
        {
            case "version" or "--version": Console.WriteLine(Commands.Version); return 0;
            case "get":
                var now = options.Remove("--now");
                if (options.Count > 0) throw new UsageException($"unknown option {options[0]}");
                var info = Read(Session(), artwork);
                Console.WriteLine(info == null ? "null" : ToJson(info, now, micros, null));
                return 0;
            case "stream":
                var noDiff = options.Remove("--no-diff");
                var debounce = 0;
                foreach (var o in options.ToList())
                    if (o.StartsWith("--debounce=") && int.TryParse(o[11..], out debounce)) options.Remove(o);
                if (options.Count > 0) throw new UsageException($"unknown option {options[0]}");
                return Stream(noDiff, Math.Max(debounce, 250), micros, artwork);
            case "play": return Send("play", s => s.TryPlayAsync(), tool);
            case "pause": return Send("pause", s => s.TryPauseAsync(), tool);
            case "toggle-play-pause": return Send("toggle", s => s.TryTogglePlayPauseAsync(), tool);
            case "stop": return Send("stop", s => s.TryStopAsync(), tool);
            case "next-track": return Send("next", s => s.TrySkipNextAsync(), tool);
            case "previous-track": return Send("previous", s => s.TrySkipPreviousAsync(), tool);
            case "seek":
                if (options.Count != 1 || !double.TryParse(options[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
                    throw new UsageException("seek needs a position in seconds");
                return Send("seek", s => s.TryChangePlaybackPositionAsync(Ticks(seconds)), tool);
            case "shuffle":
                if (options.Count != 1 || options[0] is not ("on" or "off" or "songs" or "albums")) throw new UsageException("shuffle needs on or off");
                return Send("shuffle", s => s.TryChangeShuffleActiveAsync(options[0] != "off"), tool);
            case "repeat":
                var mode = options.Count == 1 ? options[0] switch
                {
                    "off" => Windows.Media.MediaPlaybackAutoRepeatMode.None,
                    "one" or "track" => Windows.Media.MediaPlaybackAutoRepeatMode.Track,
                    "all" or "list" => Windows.Media.MediaPlaybackAutoRepeatMode.List,
                    _ => throw new UsageException("repeat needs off, one or all"),
                } : throw new UsageException("repeat needs off, one or all");
                return Send("repeat", s => s.TryChangeAutoRepeatModeAsync(mode), tool);
            case "speed":
                if (options.Count != 1 || !double.TryParse(options[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var rate) || rate <= 0)
                    throw new UsageException("speed needs a positive rate, e.g. 1.5");
                return Send("speed", s => s.TryChangePlaybackRateAsync(rate), tool);
            default: throw new UsageException($"unknown command '{args[0]}'");
        }
    }

    // Key/value pairs in media-control's field names; null values are left out.
    static List<(string Key, string Json)> Fields(NowPlaying p, bool now, bool micros)
    {
        string Time(double? s) => micros && s is double v ? ((long)(v * 1_000_000)).ToString(CultureInfo.InvariantCulture) : Num(s);
        var fields = new List<(string, string)>
        {
            ("bundleIdentifier", JsonString(p.App)),
            ("playing", p.Playing ? "true" : "false"),
            ("playbackStatus", JsonString(p.PlaybackStatus)),
            ("title", JsonString(p.Title)),
            ("artist", JsonString(p.Artist)),
            ("album", JsonString(p.Album)),
            ("albumArtist", JsonString(p.AlbumArtist)),
            ("genre", JsonString(p.Genre)),
            ("trackNumber", p.TrackNumber?.ToString(CultureInfo.InvariantCulture) ?? "null"),
            ("totalTrackCount", p.TrackCount?.ToString(CultureInfo.InvariantCulture) ?? "null"),
            ("duration", Time(p.Duration)),
            ("elapsedTime", Time(p.Elapsed)),
            ("playbackRate", Num(p.Rate)),
            ("shuffleMode", p.Shuffle is bool sh ? (sh ? "\"on\"" : "\"off\"") : "null"),
            ("repeatMode", JsonString(p.Repeat switch { "Track" => "one", "List" => "all", "None" => "off", _ => null })),
            ("timestamp", JsonString(p.Timestamp?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture))),
            ("artworkMimeType", JsonString(p.ArtworkMime)),
            ("artworkData", p.Artwork == null ? "null" : JsonString(Convert.ToBase64String(p.Artwork))),
        };
        if (now) fields.Insert(12, ("elapsedTimeNow", Time(ElapsedNow(p))));
        return fields.Where(f => f.Item2 != "null").ToList();
    }

    static string ToJson(NowPlaying p, bool now, bool micros, HashSet<string> only)
    {
        var fields = Fields(p, now, micros).Where(f => only == null || only.Contains(f.Key));
        return "{" + string.Join(",", fields.Select(f => $"\"{f.Key}\":{f.Json}")) + "}";
    }

    static int Stream(bool noDiff, int debounceMs, bool micros, bool artwork)
    {
        Dictionary<string, string> last = null;
        var lastArtworkKey = "";
        byte[] lastArtwork = null;
        while (true)
        {
            var info = Read(Session(), false);
            if (info != null && artwork)
            {
                // Artwork is big: fetch it again only when the track changes.
                var key = $"{info.App}|{info.Title}|{info.Artist}|{info.Album}";
                if (key != lastArtworkKey) { lastArtwork = Read(Session(), true)?.Artwork; lastArtworkKey = key; }
                info.Artwork = lastArtwork;
                if (lastArtwork != null) info.ArtworkMime ??= "image/png";
            }
            var current = info == null ? new Dictionary<string, string>() : Fields(info, false, micros).ToDictionary(f => f.Key, f => f.Json);
            if (last == null || !current.OrderBy(k => k.Key).SequenceEqual(last.OrderBy(k => k.Key)))
            {
                string payload;
                var diff = !noDiff && last != null;
                if (diff)
                {
                    var changed = current.Where(kv => !last.TryGetValue(kv.Key, out var old) || old != kv.Value).Select(kv => $"\"{kv.Key}\":{kv.Value}")
                        .Concat(last.Keys.Where(k => !current.ContainsKey(k)).Select(k => $"\"{k}\":null"));
                    payload = "{" + string.Join(",", changed) + "}";
                }
                else payload = "{" + string.Join(",", current.Select(kv => $"\"{kv.Key}\":{kv.Value}")) + "}";
                Console.WriteLine($"{{\"type\":\"data\",\"diff\":{(diff ? "true" : "false")},\"payload\":{payload}}}");
                Console.Out.Flush();
                last = current;
            }
            Thread.Sleep(debounceMs);
        }
    }

    // ---- nowplaying-cli -------------------------------------------------------------------------

    static int RunNowPlaying(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine("""
                Usage: nowplaying-cli [command]

                Commands:
                  get [properties...]   Print each property on its own line ("null" if unavailable)
                  get-raw               Print all properties as JSON
                  play | pause | togglePlayPause | next | previous
                  seek <secs>           Seek to a position
                  skip <secs>           Skip forward (or back with a negative number)

                Properties: title, artist, album, albumArtist, duration, elapsedTime, elapsedTimeNow,
                playbackRate, isPlaying, trackNumber, totalTrackCount, genre, artworkData, artworkMIMEType,
                timestamp, bundleIdentifier
                """);
            return args.Length == 0 ? 2 : 0;
        }
        const string tool = "nowplaying-cli";
        switch (args[0])
        {
            case "get":
                var keys = args.Skip(1).ToList();
                if (keys.Count == 0) keys = ["title", "artist", "album", "duration", "elapsedTime"];
                var info = Read(Session(), keys.Contains("artworkData") || keys.Contains("artworkMIMEType"));
                foreach (var key in keys) Console.WriteLine(info == null ? "null" : Property(info, key) ?? "null");
                return 0;
            case "get-raw":
                var raw = Read(Session(), true);
                Console.WriteLine(raw == null ? "{}" : ToJson(raw, true, false, null));
                return 0;
            case "play": return Send("play", s => s.TryPlayAsync(), tool);
            case "pause": return Send("pause", s => s.TryPauseAsync(), tool);
            case "togglePlayPause": return Send("togglePlayPause", s => s.TryTogglePlayPauseAsync(), tool);
            case "next": return Send("next", s => s.TrySkipNextAsync(), tool);
            case "previous": return Send("previous", s => s.TrySkipPreviousAsync(), tool);
            case "seek" or "skip":
                if (args.Length != 2 || !double.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var secs))
                    throw new UsageException($"{args[0]} needs a number of seconds");
                if (args[0] == "seek") return Send("seek", s => s.TryChangePlaybackPositionAsync(Ticks(secs)), tool);
                var session = Session();
                var now = session == null ? null : ElapsedNow(Read(session, false));
                if (now is not double position) { Console.Error.WriteLine($"{tool}: nothing is playing"); return 1; }
                return Send("skip", s => s.TryChangePlaybackPositionAsync(Ticks(Math.Max(0, position + secs))), tool);
            default: throw new UsageException($"unknown command '{args[0]}'");
        }
    }

    static string Property(NowPlaying p, string key) => key.ToLowerInvariant() switch
    {
        "title" => p.Title,
        "artist" => p.Artist,
        "album" => p.Album,
        "albumartist" => p.AlbumArtist,
        "genre" => p.Genre,
        "duration" => p.Duration is null ? null : Num(p.Duration),
        "elapsedtime" => p.Elapsed is null ? null : Num(p.Elapsed),
        "elapsedtimenow" => ElapsedNow(p) is double n ? Num(n) : null,
        "playbackrate" => Num(p.Playing ? p.Rate : 0),
        "isplaying" => p.Playing ? "1" : "0",
        "tracknumber" => p.TrackNumber?.ToString(CultureInfo.InvariantCulture),
        "totaltrackcount" => p.TrackCount?.ToString(CultureInfo.InvariantCulture),
        "artworkdata" => p.Artwork == null ? null : Convert.ToBase64String(p.Artwork),
        "artworkmimetype" => p.ArtworkMime,
        "timestamp" => p.Timestamp?.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss +0000", CultureInfo.InvariantCulture),
        "bundleidentifier" or "clientbundleidentifier" => p.App,
        _ => null,
    };
}
