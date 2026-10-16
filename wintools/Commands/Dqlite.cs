using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace WinTools;

// dqlite: embeddable, replicated and fault-tolerant SQLite. Each node keeps its databases in
// SQLite (Windows' built-in winsqlite3.dll) and the nodes agree on every write through Raft
// (leader election, log replication, majority commit, single-server membership changes), so the
// data survives losing a minority of nodes. Writes are replicated as SQL statements (dqlite
// replicates WAL pages, which also covers non-deterministic SQL such as random(); here statements
// should be deterministic). The tools are go-dqlite's: dqlite-demo runs a node with the demo's
// key/value HTTP API (--db for the cluster, --api for HTTP, --join to join a cluster), and dqlite is
// the SQL shell for a cluster. A node stopped with Ctrl+C hands its role over first, like go-dqlite's
// app.Handover, so a two-node cluster keeps working on the remaining node.
static class Dqlite
{
    const string Version = "1.18.7";

    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("dqlite-demo", "Run a dqlite node with the demo key/value HTTP API (Raft-replicated SQLite)", RunDemo);
        Commands.Add("dqlite", "SQL shell for a dqlite cluster", RunShell);
    }

    // ---- messages ----

    sealed class Entry
    {
        public long Term;
        public string Kind = "sql";   // sql, cfg, noop
        public string Db, Data;
    }

    sealed class Msg
    {
        public string Type, From, Leader, Candidate, Error, Db, Sql, Address;
        public long Term, LastIndex, LastTerm, PrevIndex, PrevTerm, Commit, Match, Changes, LastId;
        public bool Granted, Success;
        public List<Entry> Entries;
        public List<string> Members, Columns;
        public List<List<string>> Rows;

        public byte[] Encode()
        {
            var ms = new MemoryStream();
            using (var w = new Utf8JsonWriter(ms))
            {
                w.WriteStartObject();
                void S(string k, string v) { if (v != null) w.WriteString(k, v); }
                void N(string k, long v) { if (v != 0) w.WriteNumber(k, v); }
                S("type", Type); S("from", From); S("leader", Leader); S("candidate", Candidate); S("error", Error);
                S("db", Db); S("sql", Sql); S("address", Address);
                N("term", Term); N("lastIndex", LastIndex); N("lastTerm", LastTerm); N("prevIndex", PrevIndex); N("prevTerm", PrevTerm);
                N("commit", Commit); N("match", Match); N("changes", Changes); N("lastId", LastId);
                if (Granted) w.WriteBoolean("granted", true);
                if (Success) w.WriteBoolean("success", true);
                if (Entries != null)
                {
                    w.WriteStartArray("entries");
                    foreach (var e in Entries)
                    {
                        w.WriteStartObject();
                        w.WriteNumber("term", e.Term);
                        w.WriteString("kind", e.Kind);
                        if (e.Db != null) w.WriteString("db", e.Db);
                        if (e.Data != null) w.WriteString("data", e.Data);
                        w.WriteEndObject();
                    }
                    w.WriteEndArray();
                }
                void L(string k, List<string> l) { if (l == null) return; w.WriteStartArray(k); foreach (var x in l) { if (x == null) w.WriteNullValue(); else w.WriteStringValue(x); } w.WriteEndArray(); }
                L("members", Members);
                L("columns", Columns);
                if (Rows != null) { w.WriteStartArray("rows"); foreach (var r in Rows) { w.WriteStartArray(); foreach (var x in r) { if (x == null) w.WriteNullValue(); else w.WriteStringValue(x); } w.WriteEndArray(); } w.WriteEndArray(); }
                w.WriteEndObject();
            }
            return ms.ToArray();
        }

        public static Msg Decode(byte[] data)
        {
            using var doc = JsonDocument.Parse(data);
            var r = doc.RootElement;
            string S(string k) => r.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            long N(string k) => r.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : 0;
            bool B(string k) => r.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.True;
            List<string> L(JsonElement e) => e.EnumerateArray().Select(x => x.ValueKind == JsonValueKind.Null ? null : x.GetString()).ToList();
            var m = new Msg
            {
                Type = S("type"), From = S("from"), Leader = S("leader"), Candidate = S("candidate"), Error = S("error"), Db = S("db"), Sql = S("sql"), Address = S("address"),
                Term = N("term"), LastIndex = N("lastIndex"), LastTerm = N("lastTerm"), PrevIndex = N("prevIndex"), PrevTerm = N("prevTerm"),
                Commit = N("commit"), Match = N("match"), Changes = N("changes"), LastId = N("lastId"), Granted = B("granted"), Success = B("success"),
            };
            if (r.TryGetProperty("entries", out var es))
                m.Entries = es.EnumerateArray().Select(e => new Entry
                {
                    Term = e.GetProperty("term").GetInt64(),
                    Kind = e.GetProperty("kind").GetString(),
                    Db = e.TryGetProperty("db", out var d) ? d.GetString() : null,
                    Data = e.TryGetProperty("data", out var x) ? x.GetString() : null,
                }).ToList();
            if (r.TryGetProperty("members", out var ms)) m.Members = L(ms);
            if (r.TryGetProperty("columns", out var cs)) m.Columns = L(cs);
            if (r.TryGetProperty("rows", out var rs)) m.Rows = rs.EnumerateArray().Select(L).ToList();
            return m;
        }
    }

    // Sends a request to a node and returns its reply (null when unreachable).
    interface ITransport { Msg Call(string address, Msg m, int timeoutMs); }

    // Length-prefixed JSON over TCP, one connection per request.
    sealed class TcpTransport : ITransport
    {
        public Msg Call(string address, Msg m, int timeoutMs)
        {
            try
            {
                var ep = Endpoint(address);
                using var s = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true, ReceiveTimeout = timeoutMs, SendTimeout = timeoutMs };
                if (!s.ConnectAsync(ep).Wait(Math.Min(timeoutMs, 1000))) return null;
                Write(s, m.Encode());
                return Read(s) is { } reply ? Msg.Decode(reply) : null;
            }
            catch (Exception e) when (e is SocketException or IOException or AggregateException or JsonException or ObjectDisposedException) { return null; }
        }

        public static void Write(Socket s, byte[] body)
        {
            var buf = new byte[4 + body.Length];
            BinaryPrimitives.WriteInt32BigEndian(buf, body.Length);
            body.CopyTo(buf, 4);
            s.Send(buf);
        }

        public static byte[] Read(Socket s)
        {
            var head = Exactly(s, 4);
            if (head == null) return null;
            var len = BinaryPrimitives.ReadInt32BigEndian(head);
            return len is < 0 or > 64 << 20 ? null : Exactly(s, len);
        }

        static byte[] Exactly(Socket s, int n)
        {
            var b = new byte[n];
            var got = 0;
            while (got < n)
            {
                var r = s.Receive(b, got, n - got, SocketFlags.None);
                if (r == 0) return null;
                got += r;
            }
            return b;
        }
    }

    static IPEndPoint Endpoint(string address)
    {
        var colon = address.LastIndexOf(':');
        if (colon <= 0 || !int.TryParse(address[(colon + 1)..], out var port)) throw new UsageException($"invalid address {address} (host:port)");
        var host = address[..colon].Trim('[', ']');
        var ip = IPAddress.TryParse(host, out var parsed) ? parsed : Dns.GetHostAddresses(host).First(a => a.AddressFamily == AddressFamily.InterNetwork);
        return new IPEndPoint(ip, port);
    }

    // ---- the Raft node ----

    sealed class Node : IDisposable
    {
        public readonly string Id;
        readonly ITransport net;
        readonly string dir;
        readonly object gate = new();
        readonly Store store;
        long term;
        string votedFor;
        readonly List<Entry> log = [new Entry { Term = 0, Kind = "noop" }];   // log[0] is a sentinel
        long commitIndex, lastApplied;
        public string Role = "follower";
        public string LeaderId;
        DateTime deadline;
        DateTime lastHeartbeat = DateTime.MinValue;
        readonly Dictionary<string, long> nextIndex = [], matchIndex = [];
        readonly Dictionary<long, Msg> results = [];
        readonly Random rng;
        volatile bool stopped;
        Thread ticker;

        public Node(string id, string directory, ITransport transport, int seed = 0)
        {
            Id = id;
            dir = directory;
            net = transport;
            rng = seed == 0 ? new Random() : new Random(seed);
            Directory.CreateDirectory(dir);
            store = new Store(dir);
            Load();
            ResetDeadline();
        }

        public void Start()
        {
            ticker = new Thread(Tick) { IsBackground = true };
            ticker.Start();
        }

        public void Dispose()
        {
            stopped = true;
            ticker?.Join(2000);
            store.Dispose();
        }

        public List<string> Members
        {
            get
            {
                lock (gate)
                    for (var i = log.Count - 1; i >= 1; i--)
                        if (log[i].Kind == "cfg") return log[i].Data.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                return [];
            }
        }

        public long LastIndex { get { lock (gate) return log.Count - 1; } }

        // A new cluster: this node alone, in the first log entry.
        public void Bootstrap()
        {
            lock (gate)
            {
                if (log.Count > 1) return;
                Append(new Entry { Term = 0, Kind = "cfg", Data = Id });
                commitIndex = 1;
            }
        }

        void ResetDeadline() => deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(300 + rng.Next(300));

        // ---- persistence: term/vote and the log, as JSON lines ----

        string StatePath => Path.Combine(dir, "raft-state");
        string LogPath => Path.Combine(dir, "raft-log");

        void Load()
        {
            if (File.Exists(StatePath))
            {
                var parts = File.ReadAllText(StatePath).Split('\n');
                term = long.Parse(parts[0]);
                votedFor = parts.Length > 1 && parts[1].Length > 0 ? parts[1] : null;
            }
            if (File.Exists(LogPath))
                foreach (var line in File.ReadAllLines(LogPath).Where(l => l.Length > 0))
                {
                    var m = Msg.Decode(Encoding.UTF8.GetBytes(line));
                    log.Add(m.Entries[0]);
                }
            // Applied state is rebuilt by replaying committed entries; the commit index is learned again.
            store.Reset();
        }

        void SaveState() => File.WriteAllText(StatePath, $"{term}\n{votedFor}");

        void Append(Entry e)
        {
            log.Add(e);
            File.AppendAllText(LogPath, Encoding.UTF8.GetString(new Msg { Entries = [e] }.Encode()) + "\n");
        }

        void TruncateFrom(long index)
        {
            log.RemoveRange((int)index, log.Count - (int)index);
            File.WriteAllLines(LogPath, log.Skip(1).Select(e => Encoding.UTF8.GetString(new Msg { Entries = [e] }.Encode())));
        }

        // ---- the timer ----

        void Tick()
        {
            while (!stopped)
            {
                Thread.Sleep(20);
                string role;
                lock (gate) role = Role;
                if (role == "leader")
                {
                    if (DateTime.UtcNow - lastHeartbeat > TimeSpan.FromMilliseconds(100)) Replicate();
                }
                else if (DateTime.UtcNow > deadline && Members.Contains(Id)) Elect();
                Apply();
            }
        }

        void Elect()
        {
            long myTerm, lastIdx, lastTerm;
            List<string> others;
            lock (gate)
            {
                term++;
                votedFor = Id;
                Role = "candidate";
                LeaderId = null;
                SaveState();
                ResetDeadline();
                myTerm = term;
                lastIdx = log.Count - 1;
                lastTerm = log[^1].Term;
                others = Members.Where(m => m != Id).ToList();
            }
            var votes = 1;
            var needed = (others.Count + 1) / 2 + 1;
            var replies = others.AsParallel().Select(o => net.Call(o, new Msg { Type = "vote", From = Id, Candidate = Id, Term = myTerm, LastIndex = lastIdx, LastTerm = lastTerm }, 300)).ToList();
            lock (gate)
            {
                foreach (var r in replies.Where(r => r != null))
                {
                    if (r.Term > term) { StepDown(r.Term); return; }
                    if (r.Granted) votes++;
                }
                if (Role != "candidate" || term != myTerm || votes < needed) return;
                Role = "leader";
                LeaderId = Id;
                foreach (var m in Members) { nextIndex[m] = log.Count; matchIndex[m] = 0; }
                Append(new Entry { Term = term, Kind = "noop" });   // commits earlier terms' entries
                lastHeartbeat = DateTime.MinValue;
            }
            Replicate();
        }

        void StepDown(long newTerm)
        {
            if (newTerm > term) { term = newTerm; votedFor = null; SaveState(); }
            if (Role == "leader") LeaderId = null;
            Role = "follower";
            ResetDeadline();
        }

        // Sends AppendEntries to every other member and advances the commit index.
        void Replicate()
        {
            List<(string Peer, Msg Req)> requests;
            lock (gate)
            {
                if (Role != "leader") return;
                lastHeartbeat = DateTime.UtcNow;
                requests = [];
                foreach (var peer in Members.Where(m => m != Id))
                {
                    if (!nextIndex.TryGetValue(peer, out var next)) { next = log.Count; nextIndex[peer] = next; matchIndex[peer] = 0; }
                    next = Math.Clamp(next, 1, log.Count);
                    requests.Add((peer, new Msg
                    {
                        Type = "append", From = Id, Leader = Id, Term = term, PrevIndex = next - 1, PrevTerm = log[(int)next - 1].Term,
                        Entries = log.Skip((int)next).Take(256).ToList(), Commit = commitIndex,
                    }));
                }
            }
            var replies = requests.AsParallel().Select(r => (r.Peer, r.Req, Reply: net.Call(r.Peer, r.Req, 500))).ToList();
            lock (gate)
            {
                if (Role != "leader") return;
                foreach (var (peer, req, reply) in replies)
                {
                    if (reply == null) continue;
                    if (reply.Term > term) { StepDown(reply.Term); return; }
                    if (reply.Success)
                    {
                        matchIndex[peer] = Math.Max(matchIndex.GetValueOrDefault(peer), req.PrevIndex + req.Entries.Count);
                        nextIndex[peer] = matchIndex[peer] + 1;
                    }
                    else nextIndex[peer] = Math.Max(1, Math.Min(nextIndex[peer] - 1, reply.LastIndex + 1));
                }
                AdvanceCommit();
            }
        }

        void AdvanceCommit()
        {
            var members = Members;
            for (var n = (long)log.Count - 1; n > commitIndex; n--)
            {
                if (log[(int)n].Term != term) break;
                var count = members.Count(m => m == Id ? true : matchIndex.GetValueOrDefault(m) >= n);
                if (count * 2 > members.Count) { commitIndex = n; break; }
            }
            // A leader that committed its own removal steps down.
            if (!Members.Contains(Id) && LastCfgIndex() <= commitIndex) { Role = "follower"; LeaderId = null; deadline = DateTime.MaxValue; }
        }

        long LastCfgIndex()
        {
            for (var i = log.Count - 1; i >= 1; i--) if (log[i].Kind == "cfg") return i;
            return 0;
        }

        // Applies committed entries to the databases, keeping results for waiting clients.
        void Apply()
        {
            while (true)
            {
                Entry e;
                long index;
                lock (gate)
                {
                    if (lastApplied >= commitIndex) return;
                    index = ++lastApplied;
                    e = log[(int)index];
                }
                if (e.Kind != "sql") continue;
                var result = store.Exec(e.Db, e.Data);
                lock (gate)
                {
                    if (results.ContainsKey(index)) results[index] = result;
                    Monitor.PulseAll(gate);
                }
            }
        }

        // ---- requests from other nodes and clients ----

        public Msg Handle(Msg m)
        {
            switch (m.Type)
            {
                case "vote":
                    lock (gate)
                    {
                        if (m.Term > term) StepDown(m.Term);
                        var upToDate = m.LastTerm > log[^1].Term || m.LastTerm == log[^1].Term && m.LastIndex >= log.Count - 1;
                        var grant = m.Term == term && (votedFor == null || votedFor == m.Candidate) && upToDate;
                        if (grant) { votedFor = m.Candidate; SaveState(); ResetDeadline(); }
                        return new Msg { Type = "vote_reply", Term = term, Granted = grant };
                    }
                case "append":
                    lock (gate)
                    {
                        if (m.Term < term) return new Msg { Type = "append_reply", Term = term, LastIndex = log.Count - 1 };
                        StepDown(m.Term);
                        LeaderId = m.Leader;
                        if (m.PrevIndex > log.Count - 1 || log[(int)m.PrevIndex].Term != m.PrevTerm)
                            return new Msg { Type = "append_reply", Term = term, LastIndex = Math.Min(log.Count - 1, m.PrevIndex - 1) };
                        var at = m.PrevIndex + 1;
                        foreach (var e in m.Entries ?? [])
                        {
                            if (at < log.Count && log[(int)at].Term != e.Term) TruncateFrom(at);
                            if (at >= log.Count) Append(e);
                            at++;
                        }
                        if (m.Commit > commitIndex) commitIndex = Math.Min(m.Commit, at - 1);
                        return new Msg { Type = "append_reply", Term = term, Success = true, LastIndex = log.Count - 1 };
                    }
                case "timeout_now":
                    lock (gate) deadline = DateTime.UtcNow;   // elect now (leadership transfer)
                    return new Msg { Type = "ok", Success = true };
                case "leader":
                    lock (gate) return new Msg { Type = "leader", Leader = LeaderId, Members = Members, Term = term };
                case "join" or "leave":
                    return ChangeMembership(m.Type, m.Address);
                case "exec":
                    return Exec(m.Db, m.Sql);
                case "query":
                    lock (gate) if (Role != "leader") return new Msg { Error = "not leader", Leader = LeaderId };
                    Apply();
                    return store.Query(m.Db, m.Sql);
                default:
                    return new Msg { Error = $"unknown request {m.Type}" };
            }
        }

        // Appends an entry as leader and waits until it is committed and applied.
        Msg Propose(Entry e, int timeoutMs = 10000)
        {
            long index;
            lock (gate)
            {
                if (Role != "leader") return new Msg { Error = "not leader", Leader = LeaderId };
                e.Term = term;
                Append(e);
                index = log.Count - 1;
                results[index] = null;
            }
            Replicate();
            var until = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            lock (gate)
            {
                while (DateTime.UtcNow < until)
                {
                    if (index <= lastApplied && (e.Kind != "sql" || results[index] != null))
                    {
                        var r = results[index] ?? new Msg { Success = true };
                        results.Remove(index);
                        return r;
                    }
                    if (Role != "leader" || log.Count <= index || log[(int)index].Term != e.Term) { results.Remove(index); return new Msg { Error = "leadership lost", Leader = LeaderId }; }
                    Monitor.Wait(gate, 50);
                }
                results.Remove(index);
                return new Msg { Error = "timed out waiting for a quorum" };
            }
        }

        Msg Exec(string db, string sql) => Propose(new Entry { Kind = "sql", Db = db ?? "demo", Data = sql });

        Msg ChangeMembership(string kind, string address)
        {
            List<string> members;
            lock (gate)
            {
                if (Role != "leader") return new Msg { Error = "not leader", Leader = LeaderId };
                // One change at a time: the previous configuration must be committed.
                if (LastCfgIndex() > commitIndex) return new Msg { Error = "a membership change is in progress" };
                members = Members;
            }
            if (kind == "join" && members.Contains(address)) return new Msg { Success = true, Members = members };
            if (kind == "leave" && !members.Contains(address)) return new Msg { Success = true, Members = members };
            var next = kind == "join" ? [.. members, address] : members.Where(m => m != address).ToList();
            var r = Propose(new Entry { Kind = "cfg", Data = string.Join(",", next) });
            if (r.Error == null) r.Members = next;
            return r;
        }

        // ---- leaving gracefully: give the role away so the rest keep a quorum ----

        public void Handover()
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                if (!Members.Contains(Id) || Members.Count <= 1) return;
                string leader;
                lock (gate) leader = Role == "leader" ? Id : LeaderId;
                if (leader == null) { Thread.Sleep(200); continue; }
                var r = leader == Id ? ChangeMembership("leave", Id) : net.Call(leader, new Msg { Type = "leave", Address = Id }, 5000);
                if (r != null && r.Error == null)
                {
                    // Let the most up-to-date remaining node take over at once.
                    string successor;
                    lock (gate) successor = Members.OrderByDescending(m => matchIndex.GetValueOrDefault(m)).FirstOrDefault(m => m != Id);
                    if (leader == Id && successor != null) { Replicate(); net.Call(successor, new Msg { Type = "timeout_now" }, 500); }
                    return;
                }
                Thread.Sleep(200);
            }
        }

        // Asks the cluster (through any member) to add this node.
        public bool Join(IEnumerable<string> addresses)
        {
            foreach (var start in addresses)
            {
                var target = start;
                for (var hop = 0; hop < 5 && target != null; hop++)
                {
                    var r = net.Call(target, new Msg { Type = "join", Address = Id }, 15000);
                    if (r == null) break;
                    if (r.Error == null) return true;
                    target = r.Leader ?? (r.Error == "a membership change is in progress" ? target : null);
                    if (r.Error == "a membership change is in progress") Thread.Sleep(300);
                }
            }
            return false;
        }
    }

    // ---- SQLite state machine (winsqlite3) ----

    sealed class Store(string dir) : IDisposable
    {
        readonly Dictionary<string, IntPtr> dbs = [];

        IntPtr Db(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Any(c => !char.IsLetterOrDigit(c) && c is not ('_' or '-' or '.'))) throw new IOException($"invalid database name {name}");
            lock (dbs)
            {
                if (dbs.TryGetValue(name, out var h)) return h;
                var path = Encoding.UTF8.GetBytes(Path.Combine(dir, name + ".sqlite") + "\0");
                if (sqlite3_open_v2(path, out h, 0x2 | 0x4, IntPtr.Zero) != 0) throw new IOException($"cannot open database {name}");
                dbs[name] = h;
                return h;
            }
        }

        // The databases are rebuilt from the log on start.
        public void Reset()
        {
            foreach (var f in Directory.GetFiles(dir, "*.sqlite*")) File.Delete(f);
        }

        public Msg Exec(string name, string sql)
        {
            try
            {
                var db = Db(name);
                lock (dbs)
                {
                    var rc = sqlite3_exec(db, Encoding.UTF8.GetBytes(sql + "\0"), IntPtr.Zero, IntPtr.Zero, out var err);
                    if (rc != 0)
                    {
                        var message = Marshal.PtrToStringUTF8(err) ?? $"error {rc}";
                        if (err != IntPtr.Zero) sqlite3_free(err);
                        return new Msg { Error = message };
                    }
                    return new Msg { Success = true, Changes = sqlite3_changes(db), LastId = sqlite3_last_insert_rowid(db) };
                }
            }
            catch (IOException e) { return new Msg { Error = e.Message }; }
        }

        public Msg Query(string name, string sql)
        {
            try
            {
                var db = Db(name ?? "demo");
                lock (dbs)
                {
                    var bytes = Encoding.UTF8.GetBytes(sql + "\0");
                    if (sqlite3_prepare_v2(db, bytes, -1, out var stmt, IntPtr.Zero) != 0)
                        return new Msg { Error = Marshal.PtrToStringUTF8(sqlite3_errmsg(db)) };
                    try
                    {
                        var n = sqlite3_column_count(stmt);
                        var r = new Msg { Success = true, Columns = [], Rows = [] };
                        for (var i = 0; i < n; i++) r.Columns.Add(Marshal.PtrToStringUTF8(sqlite3_column_name(stmt, i)));
                        int rc;
                        while ((rc = sqlite3_step(stmt)) == 100)
                        {
                            var row = new List<string>();
                            for (var i = 0; i < n; i++)
                                row.Add(sqlite3_column_type(stmt, i) == 5 ? null : Marshal.PtrToStringUTF8(sqlite3_column_text(stmt, i), sqlite3_column_bytes(stmt, i)));
                            r.Rows.Add(row);
                        }
                        if (rc != 101) return new Msg { Error = Marshal.PtrToStringUTF8(sqlite3_errmsg(db)) };
                        return r;
                    }
                    finally { sqlite3_finalize(stmt); }
                }
            }
            catch (IOException e) { return new Msg { Error = e.Message }; }
        }

        public void Dispose()
        {
            lock (dbs) { foreach (var h in dbs.Values) sqlite3_close(h); dbs.Clear(); }
        }
    }

    const string Sqlite = "winsqlite3.dll";
    [DllImport(Sqlite, CallingConvention = CallingConvention.StdCall)] static extern int sqlite3_open_v2(byte[] filename, out IntPtr db, int flags, IntPtr vfs);
    [DllImport(Sqlite, CallingConvention = CallingConvention.StdCall)] static extern int sqlite3_close(IntPtr db);
    [DllImport(Sqlite, CallingConvention = CallingConvention.StdCall)] static extern int sqlite3_exec(IntPtr db, byte[] sql, IntPtr callback, IntPtr arg, out IntPtr err);
    [DllImport(Sqlite, CallingConvention = CallingConvention.StdCall)] static extern void sqlite3_free(IntPtr p);
    [DllImport(Sqlite, CallingConvention = CallingConvention.StdCall)] static extern int sqlite3_changes(IntPtr db);
    [DllImport(Sqlite, CallingConvention = CallingConvention.StdCall)] static extern long sqlite3_last_insert_rowid(IntPtr db);
    [DllImport(Sqlite, CallingConvention = CallingConvention.StdCall)] static extern int sqlite3_prepare_v2(IntPtr db, byte[] sql, int bytes, out IntPtr stmt, IntPtr tail);
    [DllImport(Sqlite, CallingConvention = CallingConvention.StdCall)] static extern int sqlite3_step(IntPtr stmt);
    [DllImport(Sqlite, CallingConvention = CallingConvention.StdCall)] static extern int sqlite3_finalize(IntPtr stmt);
    [DllImport(Sqlite, CallingConvention = CallingConvention.StdCall)] static extern int sqlite3_column_count(IntPtr stmt);
    [DllImport(Sqlite, CallingConvention = CallingConvention.StdCall)] static extern IntPtr sqlite3_column_name(IntPtr stmt, int i);
    [DllImport(Sqlite, CallingConvention = CallingConvention.StdCall)] static extern int sqlite3_column_type(IntPtr stmt, int i);
    [DllImport(Sqlite, CallingConvention = CallingConvention.StdCall)] static extern IntPtr sqlite3_column_text(IntPtr stmt, int i);
    [DllImport(Sqlite, CallingConvention = CallingConvention.StdCall)] static extern int sqlite3_column_bytes(IntPtr stmt, int i);
    [DllImport(Sqlite, CallingConvention = CallingConvention.StdCall)] static extern IntPtr sqlite3_errmsg(IntPtr db);

    // ---- dqlite-demo ----

    static int RunDemo(string[] args)
    {
        string api = null, db = null, dir = null;
        var joins = new List<string>();
        var verbose = false;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            var key = a.Contains('=') ? a[..a.IndexOf('=')] : a;
            string Value() => a.Contains('=') ? a[(a.IndexOf('=') + 1)..] : ++i < args.Length ? args[i] : throw new UsageException($"{a} needs a value");
            switch (key)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Demo application using dqlite

                        Usage:
                          dqlite-demo [flags]

                        Flags:
                          -a, --api string     address used to expose the demo API
                          -d, --db string      address used for internal database replication
                          -j, --join strings   database addresses of existing nodes
                          -D, --dir string     data directory (default: %TEMP%\dqlite-demo)
                          -v, --verbose        verbose logging
                          -h, --help           help for dqlite-demo

                        The API: PUT /<key> with the value as the body stores it ("done"), GET /<key> returns it.
                        """);
                    return 0;
                case "--version": Console.WriteLine(Version); return 0;
                case "-a" or "--api": api = Value(); break;
                case "-d" or "--db": db = Value(); break;
                case "-j" or "--join": joins.AddRange(Value().Split(',', StringSplitOptions.RemoveEmptyEntries)); break;
                case "-D" or "--dir": dir = Value(); break;
                case "-v" or "--verbose": verbose = true; break;
                case "--self-test": return SelfTest();
                default: throw new UsageException($"unknown flag: {a}");
            }
        }
        if (api == null || db == null) throw new UsageException("both --api and --db are required (see dqlite-demo --help)");
        dir ??= Path.Combine(Path.GetTempPath(), "dqlite-demo");
        dir = Path.Combine(dir, db.Replace(':', '_'));
        var node = new Node(db, dir, new TcpTransport());
        var raft = new TcpListener(Endpoint(db));
        raft.Start();
        new Thread(() => ServeRaft(raft, node)) { IsBackground = true }.Start();
        if (joins.Count == 0) node.Bootstrap();
        node.Start();
        if (joins.Count > 0 && !node.Members.Contains(db) && !node.Join(joins))
        {
            Console.Error.WriteLine($"Error: can't join the cluster at {string.Join(", ", joins)}");
            return 1;
        }
        // The demo's table, created through the cluster once a leader exists.
        for (var i = 0; i < 100; i++)
        {
            var r = Client.Exec(new TcpTransport(), node.Members.Count > 0 ? node.Members : [db], "demo", "CREATE TABLE IF NOT EXISTS model (key TEXT, value TEXT, UNIQUE(key))");
            if (r.Error == null) break;
            Thread.Sleep(200);
        }
        var http = new TcpListener(Endpoint(api));
        http.Start();
        if (verbose) Console.Error.WriteLine($"dqlite-demo: node {db} serving the API on {api}, members {string.Join(",", node.Members)}");
        using var stop = new ManualResetEventSlim();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Set(); };
        new Thread(() => ServeHttp(http, node, verbose)) { IsBackground = true }.Start();
        stop.Wait();
        node.Handover();
        http.Stop();
        raft.Stop();
        node.Dispose();
        return 0;
    }

    static void ServeRaft(TcpListener listener, Node node)
    {
        while (true)
        {
            Socket s;
            try { s = listener.AcceptSocket(); }
            catch (Exception e) when (e is SocketException or ObjectDisposedException or InvalidOperationException) { return; }
            ThreadPool.QueueUserWorkItem(_ =>
            {
                using (s)
                {
                    try
                    {
                        s.ReceiveTimeout = 15000;
                        if (TcpTransport.Read(s) is not { } req) return;
                        TcpTransport.Write(s, node.Handle(Msg.Decode(req)).Encode());
                    }
                    catch (Exception e) when (e is SocketException or IOException or JsonException or ObjectDisposedException) { }
                }
            });
        }
    }

    // A minimal HTTP/1.1 server for the demo's GET/PUT /<key>.
    static void ServeHttp(TcpListener listener, Node node, bool verbose)
    {
        while (true)
        {
            Socket s;
            try { s = listener.AcceptSocket(); }
            catch (Exception e) when (e is SocketException or ObjectDisposedException or InvalidOperationException) { return; }
            ThreadPool.QueueUserWorkItem(_ =>
            {
                using (s)
                using (var stream = new NetworkStream(s))
                {
                    try
                    {
                        var head = new StringBuilder();
                        while (!head.ToString().EndsWith("\r\n\r\n") && head.Length < 16384)
                        {
                            var b = stream.ReadByte();
                            if (b < 0) return;
                            head.Append((char)b);
                        }
                        var lines = head.ToString().Split("\r\n");
                        var request = lines[0].Split(' ');
                        if (request.Length < 2) return;
                        var length = lines.Where(l => l.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)).Select(l => int.Parse(l[15..].Trim())).FirstOrDefault();
                        var body = new byte[length];
                        for (var got = 0; got < length;) { var r = stream.Read(body, got, length - got); if (r <= 0) break; got += r; }
                        var key = Uri.UnescapeDataString(request[1].TrimStart('/'));
                        string result;
                        var status = "200 OK";
                        var members = node.Members;
                        var esc = key.Replace("'", "''");
                        switch (request[0])
                        {
                            case "GET":
                            {
                                var r = Client.Query(new TcpTransport(), members, "demo", $"SELECT value FROM model WHERE key = '{esc}'");
                                if (r.Error != null) { status = "500 Internal Server Error"; result = "Error: " + r.Error; }
                                else result = r.Rows.FirstOrDefault()?.FirstOrDefault() ?? "";
                                break;
                            }
                            case "PUT" or "POST":
                            {
                                var value = Encoding.UTF8.GetString(body).Replace("'", "''");
                                var r = Client.Exec(new TcpTransport(), members, "demo", $"INSERT OR REPLACE INTO model(key, value) VALUES('{esc}', '{value}')");
                                if (r.Error != null) { status = "500 Internal Server Error"; result = "Error: " + r.Error; }
                                else result = "done";
                                break;
                            }
                            default:
                                status = "405 Method Not Allowed";
                                result = "unsupported method";
                                break;
                        }
                        if (verbose) Console.Error.WriteLine($"{request[0]} /{key} -> {status}");
                        var bytes = Encoding.UTF8.GetBytes(result + "\n");
                        stream.Write(Encoding.ASCII.GetBytes($"HTTP/1.1 {status}\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n"));
                        stream.Write(bytes);
                    }
                    catch (Exception e) when (e is SocketException or IOException or FormatException) { }
                }
            });
        }
    }

    // ---- clients: find the leader and send it the request ----

    static class Client
    {
        public static Msg Exec(ITransport net, IEnumerable<string> servers, string db, string sql) => Send(net, servers, new Msg { Type = "exec", Db = db, Sql = sql });
        public static Msg Query(ITransport net, IEnumerable<string> servers, string db, string sql) => Send(net, servers, new Msg { Type = "query", Db = db, Sql = sql });

        static Msg Send(ITransport net, IEnumerable<string> servers, Msg m)
        {
            Msg last = null;
            foreach (var start in servers)
            {
                var target = start;
                for (var hop = 0; hop < 4 && target != null; hop++)
                {
                    var r = net.Call(target, m, 15000);
                    if (r == null) break;
                    if (r.Error is "not leader")
                    {
                        last = r;
                        if (r.Leader == null) { Thread.Sleep(300); continue; }   // election in progress
                        target = r.Leader;
                        continue;
                    }
                    return r;
                }
            }
            return last?.Error == "not leader" ? new Msg { Error = "no leader available" } : last ?? new Msg { Error = "no node reachable" };
        }
    }

    // ---- the dqlite shell ----

    static int RunShell(string[] args)
    {
        var servers = new List<string>();
        string db = null, format = "tabular";
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            var key = a.Contains('=') ? a[..a.IndexOf('=')] : a;
            string Value() => a.Contains('=') ? a[(a.IndexOf('=') + 1)..] : ++i < args.Length ? args[i] : throw new UsageException($"{a} needs a value");
            switch (key)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Standard dqlite shell

                        Usage:
                          dqlite -s <servers> <database> [flags]

                        Flags:
                          -s, --servers strings   comma-separated list of node addresses
                          -f, --format string     output format: tabular, json or csv (default "tabular")
                          -h, --help              help for dqlite

                        Reads SQL statements from standard input. Dot commands: .leader, .cluster.
                        """);
                    return 0;
                case "-s" or "--servers": servers.AddRange(Value().Split(',', StringSplitOptions.RemoveEmptyEntries)); break;
                case "-f" or "--format": format = Value(); break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unknown flag: {a}");
                    db = a;
                    break;
            }
        }
        if (servers.Count == 0 || db == null) throw new UsageException("usage: dqlite -s <servers> <database>");
        var net = new TcpTransport();
        var interactive = !Console.IsInputRedirected;
        var status = 0;
        while (true)
        {
            if (interactive) Console.Write("dqlite> ");
            var line = Console.ReadLine();
            if (line == null) break;
            line = line.Trim().TrimEnd(';');
            if (line.Length == 0) continue;
            if (line is ".leader" or ".cluster")
            {
                var info = servers.Select(s => net.Call(s, new Msg { Type = "leader" }, 3000)).FirstOrDefault(r => r != null);
                if (info == null) { Console.Error.WriteLine("Error: no node reachable"); status = 1; continue; }
                Console.WriteLine(line == ".leader" ? info.Leader ?? "(no leader)" : string.Join("\n", info.Members.Select(m => $"{m}|{(m == info.Leader ? "leader" : "voter")}")));
                continue;
            }
            var isQuery = System.Text.RegularExpressions.Regex.IsMatch(line, @"^\s*(SELECT|PRAGMA|EXPLAIN|WITH|VALUES)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var r2 = isQuery ? Client.Query(net, servers, db, line) : Client.Exec(net, servers, db, line);
            if (r2.Error != null) { Console.Error.WriteLine($"Error: {r2.Error}"); status = 1; continue; }
            if (!isQuery) continue;
            switch (format)
            {
                case "json":
                {
                    var ms = new MemoryStream();
                    using (var w = new Utf8JsonWriter(ms))
                    {
                        w.WriteStartArray();
                        foreach (var row in r2.Rows)
                        {
                            w.WriteStartObject();
                            for (var c = 0; c < r2.Columns.Count; c++) { if (row[c] == null) w.WriteNull(r2.Columns[c]); else w.WriteString(r2.Columns[c], row[c]); }
                            w.WriteEndObject();
                        }
                        w.WriteEndArray();
                    }
                    Console.WriteLine(Encoding.UTF8.GetString(ms.ToArray()));
                    break;
                }
                case "csv":
                    foreach (var row in r2.Rows) Console.WriteLine(string.Join(",", row.Select(v => v == null ? "" : v.Contains(',') || v.Contains('"') ? "\"" + v.Replace("\"", "\"\"") + "\"" : v)));
                    break;
                default:
                    foreach (var row in r2.Rows) Console.WriteLine(string.Join("|", row.Select(v => v ?? "")));
                    break;
            }
        }
        return status;
    }

    // ---- self-test: three nodes over an in-memory network ----

    sealed class MemoryNet : ITransport
    {
        public readonly Dictionary<string, Node> Nodes = [];
        public readonly HashSet<string> Down = [];

        public Msg Call(string address, Msg m, int timeoutMs)
        {
            Node n;
            lock (Nodes) { if (Down.Contains(address) || m.From != null && Down.Contains(m.From) || !Nodes.TryGetValue(address, out n)) return null; }
            // Round-trip through the wire format, like the TCP transport.
            return Msg.Decode(n.Handle(Msg.Decode(m.Encode())).Encode());
        }
    }

    static int SelfTest()
    {
        var root = Path.Combine(Path.GetTempPath(), "dqlite-selftest-" + Environment.ProcessId);
        var net = new MemoryNet();
        var failures = 0;
        void Check(bool ok, string what) { Console.WriteLine($"{(ok ? "ok" : "FAIL")}: {what}"); if (!ok) failures++; }
        string[] ids = ["n1", "n2", "n3"];
        var nodes = ids.Select((id, k) => new Node(id, Path.Combine(root, id), net, seed: k + 1)).ToList();
        try
        {
            lock (net.Nodes) foreach (var n in nodes) net.Nodes[n.Id] = n;
            nodes[0].Bootstrap();
            foreach (var n in nodes) n.Start();
            Wait(() => nodes[0].Role == "leader", 5000);
            Check(nodes[0].Role == "leader", "the bootstrap node elects itself");
            Check(nodes[1].Join(["n1"]) && nodes[2].Join(["n1"]), "two nodes join");
            Check(nodes[0].Members.Count == 3, "the cluster has three members");
            var r = Client.Exec(net, ids, "demo", "CREATE TABLE model (key TEXT, value TEXT, UNIQUE(key)); INSERT INTO model VALUES('my-key', 'my-value')");
            Check(r.Error == null, "a write commits on a quorum" + (r.Error != null ? $" ({r.Error})" : ""));
            // The leader goes away: the other two elect a new one and still have the data.
            lock (net.Nodes) net.Down.Add("n1");
            Wait(() => nodes.Skip(1).Any(n => n.Role == "leader"), 5000);
            Check(nodes.Skip(1).Any(n => n.Role == "leader"), "a new leader is elected after the leader fails");
            var q = Client.Query(net, ["n2", "n3"], "demo", "SELECT value FROM model WHERE key = 'my-key'");
            Check(q.Error == null && q.Rows.Count == 1 && q.Rows[0][0] == "my-value", "the data survives on the new leader");
            var w2 = Client.Exec(net, ["n2", "n3"], "demo", "INSERT OR REPLACE INTO model VALUES('k2', 'v2')");
            Check(w2.Error == null, "writes continue with two of three nodes");
            // The old leader comes back as a follower and catches up.
            lock (net.Nodes) net.Down.Remove("n1");
            Wait(() => nodes[0].LastIndex == nodes[1].LastIndex && nodes[0].Role == "follower", 5000);
            Check(nodes[0].Role == "follower" && nodes[0].LastIndex == nodes[1].LastIndex, "the old leader rejoins as a follower and catches up");
            // A leader that stops gracefully hands over: it leaves the configuration first.
            var leader = nodes.First(n => n.Role == "leader");
            leader.Handover();
            lock (net.Nodes) net.Down.Add(leader.Id);
            var rest = nodes.Where(n => n != leader).ToList();
            Wait(() => rest.Any(n => n.Role == "leader"), 5000);
            Check(rest.All(n => n.Members.Count == 2 && !n.Members.Contains(leader.Id)), "a handover removes the leaving node from the cluster");
            var q2 = Client.Query(net, rest.Select(n => n.Id), "demo", "SELECT count(*) FROM model");
            Check(q2.Error == null && q2.Rows[0][0] == "2", "the remaining nodes serve the data after the handover");
        }
        finally
        {
            foreach (var n in nodes) n.Dispose();
            try { Directory.Delete(root, true); } catch (IOException) { }
        }
        return failures == 0 ? 0 : 1;
    }

    static void Wait(Func<bool> condition, int ms)
    {
        var until = DateTime.UtcNow.AddMilliseconds(ms);
        while (!condition() && DateTime.UtcNow < until) Thread.Sleep(20);
    }
}
