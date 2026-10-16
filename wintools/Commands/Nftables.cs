using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace WinTools;

// nft (nftables) on Windows Firewall. The firewall is one table, "inet filter", with an input chain
// (inbound rules) and an output chain (outbound rules) whose policies are the profiles' default
// inbound/outbound actions. `nft list` reads every rule straight from the registry and prints it in
// nft syntax (Windows' own rules carry their name, program or service as the comment); `add`/
// `insert`/`delete`/`flush` and `-f` rule files change rules through `netsh advfirewall`. Rules
// made here are named "nft: <chain> <rule>" so they read back exactly; Windows' own rules are
// listed but never deleted. Windows Firewall is stateful by itself (ct state matches are implied),
// keeps no per-rule counters, doesn't filter forwarded traffic and does no NAT, so forward, nat and
// other tables are refused with an explanation.
static class Nftables
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("nft", "Administer Windows Firewall rules with nftables syntax", Run);

    const string Version = "1.1.7";
    const string RulesKey = @"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy";
    const string PolicyRulesKey = @"SOFTWARE\Policies\Microsoft\WindowsFirewall";
    const string Prefix = "nft: ";

    sealed class Rule
    {
        public string Chain;                 // "input" / "output"
        public string Protocol;              // tcp, udp, icmp, icmpv6, a number, or null (any)
        public string IcmpType;
        public List<string> SAddr = new(), DAddr = new(), SPort = new(), DPort = new();
        public string Family;                // "ip", "ip6" or null (both)
        public string Verdict = "accept";
        public string Comment, App, Service, RegistryName;
        public bool Ours;
        public int Handle;

        public string Text()
        {
            var parts = new List<string>();
            string Set(List<string> items) => items.Count == 1 ? items[0] : "{ " + string.Join(", ", items) + " }";
            var fam = Family ?? (SAddr.Concat(DAddr).Any(a => a.Contains(':')) ? "ip6" : "ip");
            if (SAddr.Count > 0) parts.Add($"{fam} saddr {Set(SAddr)}");
            if (DAddr.Count > 0) parts.Add($"{fam} daddr {Set(DAddr)}");
            if (Protocol is "tcp" or "udp")
            {
                if (SPort.Count > 0) parts.Add($"{Protocol} sport {Set(SPort)}");
                if (DPort.Count > 0) parts.Add($"{Protocol} dport {Set(DPort)}");
                if (SPort.Count == 0 && DPort.Count == 0) parts.Add($"meta l4proto {Protocol}");
            }
            else if (Protocol is "icmp" or "icmpv6") parts.Add(IcmpType != null ? $"{Protocol} type {IcmpType}" : $"meta l4proto {(Protocol == "icmp" ? "icmp" : "ipv6-icmp")}");
            else if (Protocol != null) parts.Add($"meta l4proto {Protocol}");
            parts.Add(Verdict);
            var comment = new List<string>();
            if (!string.IsNullOrEmpty(Comment)) comment.Add(Comment);
            if (App != null) comment.Add("program=" + App);
            if (Service != null) comment.Add("service=" + Service);
            if (comment.Count > 0) parts.Add("comment \"" + string.Join("; ", comment).Replace("\"", "'") + "\"");
            return string.Join(' ', parts);
        }
    }

    static int Run(string[] args)
    {
        bool check = false, handles = false;
        string file = null;
        var words = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (words.Count > 0) { words.Add(a); continue; }
            switch (a)
            {
                case "-h" or "--help": Usage(); return 0;
                case "-v" or "--version": Console.WriteLine($"nftables v{Version} (Commodore Bullmoose #8) - Windows Firewall, wintools {Commands.Version}"); return 0;
                case "-V": Console.WriteLine($"nftables v{Version} (Commodore Bullmoose #8)\n  cli:\t\twintools\n  json:\t\tyes\n  backend:\tWindows Firewall"); return 0;
                case "-c" or "--check": check = true; break;
                case "-a" or "--handle": handles = true; break;
                case "-j" or "--json": throw new UsageException("JSON output isn't available here; the text ruleset (nft list ruleset) is");
                case "-n" or "--numeric" or "-N" or "-s" or "--stateless" or "-e" or "--echo" or "-y" or "-t" or "--terse": break;
                case "-f" or "--file": file = ++i < args.Length ? args[i] : throw new UsageException("option requires an argument -- 'f'"); break;
                case "-i" or "--interactive": return Interactive(check);
                default:
                    if (a.StartsWith('-') && a.Length > 1) throw new UsageException($"unrecognized option '{a}'");
                    words.Add(a);
                    break;
            }
        }
        if (file != null)
        {
            var text = file == "-" ? Console.In.ReadToEnd() : File.ReadAllText(file);
            return ApplyScript(text, check, file);
        }
        if (words.Count == 0) throw new UsageException("no command specified (see nft --help)");
        // Like nft, the words may be one quoted string or several; ';' separates commands.
        var line = string.Join(' ', words);
        var status = 0;
        foreach (var command in SplitCommands(line))
            status |= Execute(Tokenize(command), check, handles, "command line");
        return status;
    }

    static int Interactive(bool check)
    {
        for (;;)
        {
            Console.Write("nft> ");
            var line = Console.ReadLine();
            if (line == null || line.Trim() is "quit" or "exit") return 0;
            if (line.Trim().Length == 0) continue;
            try { Execute(Tokenize(line), check, false, "interactive"); }
            catch (UsageException e) { Console.Error.WriteLine("Error: " + e.Message); }
            catch (InvalidOperationException e) { Console.Error.WriteLine("Error: " + e.Message); }
        }
    }

    static void Usage() => Console.WriteLine($$"""
        Usage: nft [ options ] [ cmds... ]

        nftables v{{Version}} on Windows Firewall: table inet filter, chain input (inbound rules) and
        chain output (outbound rules).

        Options:
          -h, --help                      Show this help
          -v, --version                   Show version information
          -f, --file <filename>           Read input from <filename> (- for stdin)
          -c, --check                     Check commands validity without actually applying the changes
          -a, --handle                    Output rule handle
          -i, --interactive               Read input from interactive CLI

        Commands:
          list ruleset | list tables | list table inet filter | list chain inet filter input
          add|insert rule inet filter input|output <matches> accept|drop|reject [comment "text"]
          delete rule inet filter input handle <n>      (rules added with nft only)
          flush ruleset | flush chain inet filter input (rules added with nft only)
          add chain inet filter input { type filter hook input priority 0 ; policy drop ; }

        Matches: ip|ip6 saddr|daddr <addr>[/len] or { a, b }, tcp|udp sport|dport <port>[-port] or
        { p1, p2 }, meta l4proto tcp|udp|icmp|ipv6-icmp, icmp|icmpv6 type <type>, ct state ...
        (implied: Windows Firewall is stateful).
        """);

    static IEnumerable<string> SplitCommands(string line)
    {
        var sb = new StringBuilder();
        int depth = 0;
        var quoted = false;
        foreach (var c in line)
        {
            if (c == '"') quoted = !quoted;
            if (!quoted && c == '{') depth++;
            if (!quoted && c == '}') depth--;
            if (!quoted && depth == 0 && (c == ';' || c == '\n'))
            {
                if (sb.ToString().Trim().Length > 0) yield return sb.ToString();
                sb.Clear();
                continue;
            }
            sb.Append(c);
        }
        if (sb.ToString().Trim().Length > 0) yield return sb.ToString();
    }

    static List<string> Tokenize(string s)
    {
        var tokens = new List<string>();
        var i = 0;
        while (i < s.Length)
        {
            var c = s[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (c == '#') break;
            if (c is '{' or '}' or ';' or ',')
            {
                tokens.Add(c.ToString());
                i++;
                continue;
            }
            if (c == '"')
            {
                var end = s.IndexOf('"', i + 1);
                if (end < 0) throw new UsageException("syntax error, unterminated string");
                tokens.Add(s[i..(end + 1)]);
                i = end + 1;
                continue;
            }
            var start = i;
            while (i < s.Length && !char.IsWhiteSpace(s[i]) && s[i] is not ('{' or '}' or ';' or ',' or '"')) i++;
            tokens.Add(s[start..i]);
        }
        return tokens;
    }

    // ---- commands ----

    static int Execute(List<string> t, bool check, bool handles, string where)
    {
        if (t.Count == 0) return 0;
        var verb = t[0];
        var rest = t.Skip(1).ToList();
        switch (verb)
        {
            case "list":
                if (check) return 0;
                return List(rest, handles);
            case "add" or "insert" or "create" or "replace":
                if (rest.Count == 0) throw new UsageException("syntax error, unexpected end of file");
                switch (rest[0])
                {
                    case "table":
                        Table(rest.Skip(1).ToList());
                        return 0;
                    case "chain":
                        return AddChain(rest.Skip(1).ToList(), check);
                    case "rule":
                        var (chain, body) = RuleTarget(rest.Skip(1).ToList());
                        if (verb == "replace") throw new InvalidOperationException("replace: Windows Firewall rules are unordered; delete the rule (by handle) and add the new one");
                        // position/index/handle place a rule among others; Windows Firewall has no order.
                        if (body.Count >= 2 && body[0] is "position" or "index" or "handle") body = body.Skip(2).ToList();
                        var rule = ParseRule(chain, body);
                        if (!check) AddRule(rule, body);
                        return 0;
                    case "set" or "map" or "element" or "flowtable" or "counter" or "quota" or "limit":
                        throw new InvalidOperationException($"{rest[0]}: named {rest[0]}s have no Windows Firewall equivalent; write the elements into the rule ({{ a, b }})");
                    default:
                        throw new UsageException($"syntax error, unexpected {rest[0]}");
                }
            case "delete" or "destroy":
                if (rest.Count >= 1 && rest[0] == "rule")
                {
                    var (chain, body) = RuleTarget(rest.Skip(1).ToList());
                    if (body.Count != 2 || body[0] != "handle" || !int.TryParse(body[1], out var handle))
                        throw new UsageException("syntax error: delete rule <family> <table> <chain> handle <handle>");
                    if (!check) DeleteRule(chain, handle, verb == "destroy");
                    return 0;
                }
                if (rest.Count >= 1 && rest[0] is "table" or "chain")
                    throw new InvalidOperationException($"delete {rest[0]}: the firewall's table and chains always exist; use flush to remove nft's rules");
                throw new UsageException("syntax error, unexpected " + (rest.Count > 0 ? rest[0] : "end of file"));
            case "flush":
                if (rest.Count >= 1 && rest[0] == "ruleset") { if (!check) Flush(null); return 0; }
                if (rest.Count >= 1 && rest[0] is "chain" or "table")
                {
                    var target = rest.Skip(1).ToList();
                    if (rest[0] == "table") { Table(target); if (!check) Flush(null); return 0; }
                    var (chain, _) = RuleTarget(target);
                    if (!check) Flush(chain);
                    return 0;
                }
                throw new UsageException("syntax error: flush ruleset | flush table <family> <table> | flush chain <family> <table> <chain>");
            case "table":
                return ApplyBlock(t, check);
            case "describe":
                Console.WriteLine("describe: expression types aren't documented by this nft; see nft --help for the supported matches");
                return 1;
            case "monitor":
                throw new InvalidOperationException("monitor: Windows Firewall publishes no change events to nft; use list ruleset");
            default:
                throw new UsageException($"syntax error, unexpected {verb}");
        }
    }

    static void Table(List<string> spec)
    {
        var (family, table) = spec.Count >= 2 ? (spec[0], spec[1]) : spec.Count == 1 ? ("ip", spec[0]) : throw new UsageException("syntax error: missing table name");
        if (family is not ("inet" or "ip" or "ip6"))
            throw new InvalidOperationException($"table {family} {table}: Windows Firewall only filters IP traffic (families inet, ip, ip6)");
        if (table != "filter")
            throw new InvalidOperationException($"table {family} {table}: Windows Firewall is one filter table (\"inet filter\"); it does no NAT or mangling");
    }

    static (string Chain, List<string> Body) RuleTarget(List<string> t)
    {
        // [family] table chain ...
        if (t.Count < 2) throw new UsageException("syntax error, unexpected end of file");
        var family = t[0] is "inet" or "ip" or "ip6" or "arp" or "bridge" or "netdev" ? t[0] : "ip";
        var at = family == t[0] ? 1 : 0;
        if (t.Count < at + 2) throw new UsageException("syntax error, unexpected end of file");
        Table(new List<string> { family, t[at] });
        var chain = t[at + 1].ToLowerInvariant();
        if (chain == "forward") throw new InvalidOperationException("chain forward: Windows Firewall doesn't filter forwarded traffic");
        if (chain is not ("input" or "output"))
            throw new InvalidOperationException($"chain {t[at + 1]}: Windows Firewall has two chains, input (inbound) and output (outbound)");
        return (chain, t.Skip(at + 2).ToList());
    }

    static int AddChain(List<string> t, bool check)
    {
        var (chain, body) = RuleTarget(t);
        var text = string.Join(' ', body);
        var m = System.Text.RegularExpressions.Regex.Match(text, @"policy\s+(accept|drop)");
        if (m.Success && !check) SetPolicy(chain, m.Groups[1].Value);
        return 0;
    }

    // `table inet filter { chain input { type filter hook input priority 0; policy drop; tcp dport 22 accept } }`
    static int ApplyBlock(List<string> t, bool check)
    {
        var i = 1;
        var family = t[i] is "inet" or "ip" or "ip6" or "arp" or "bridge" or "netdev" ? t[i++] : "ip";
        var table = t[i++];
        Table(new List<string> { family, table });
        if (i >= t.Count || t[i] != "{") return 0;
        i++;
        var rules = new List<(Rule, List<string>)>();
        var policies = new List<(string, string)>();
        while (i < t.Count && t[i] != "}")
        {
            if (t[i] is ";") { i++; continue; }
            if (t[i] != "chain") throw new InvalidOperationException($"{t[i]}: only chains can be declared in the Windows Firewall table");
            var chainName = t[i + 1].ToLowerInvariant();
            if (chainName is not ("input" or "output")) throw new InvalidOperationException($"chain {chainName}: Windows Firewall has input and output chains only");
            i += 2;
            if (t[i] != "{") throw new UsageException("syntax error, expected '{'");
            i++;
            var statement = new List<string>();
            var depth = 0;
            while (i < t.Count && !(t[i] == "}" && depth == 0))
            {
                if (t[i] == "{") depth++;
                if (t[i] == "}") depth--;
                if (t[i] == ";" && depth == 0)
                {
                    Statement(chainName, statement, rules, policies);
                    statement = new List<string>();
                }
                else statement.Add(t[i]);
                i++;
            }
            Statement(chainName, statement, rules, policies);
            i++;
        }
        if (check) return 0;
        foreach (var (chain, policy) in policies) SetPolicy(chain, policy);
        foreach (var (rule, body) in rules) AddRule(rule, body);
        return 0;
    }

    static void Statement(string chain, List<string> s, List<(Rule, List<string>)> rules, List<(string, string)> policies)
    {
        if (s.Count == 0) return;
        if (s[0] == "type" || s[0] == "hook" || s[0] == "priority" || s[0] == "devices") return;
        if (s[0] == "policy") { policies.Add((chain, s[1])); return; }
        rules.Add((ParseRule(chain, s), s));
    }

    static int ApplyScript(string text, bool check, string file)
    {
        // A script is a sequence of commands; table blocks span lines.
        var lines = new List<string>();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("#!") || line.StartsWith('#') || line.Length == 0) continue;
            lines.Add(line);
        }
        var joined = string.Join("\n", lines);
        var status = 0;
        var lineNo = 0;
        foreach (var command in SplitCommands(joined))
        {
            lineNo++;
            var tokens = Tokenize(command.Replace('\n', ' '));
            if (tokens.Count == 0) continue;
            if (tokens[0] == "flush" || tokens[0] == "add" || tokens[0] == "insert" || tokens[0] == "delete" || tokens[0] == "table" || tokens[0] == "list" || tokens[0] == "destroy" || tokens[0] == "create")
            {
                try { status |= Execute(tokens, check, false, file); }
                catch (UsageException e) { throw new UsageException($"{file}:{lineNo}: {e.Message}"); }
            }
            else throw new UsageException($"{file}:{lineNo}: syntax error, unexpected {tokens[0]}");
        }
        return status;
    }

    // ---- rule expressions ----

    static Rule ParseRule(string chain, List<string> t)
    {
        var r = new Rule { Chain = chain };
        var i = 0;
        List<string> Values()
        {
            if (i >= t.Count) throw new UsageException("syntax error, unexpected end of file");
            var values = new List<string>();
            if (t[i] != "{")
            {
                values.Add(t[i++]);
                while (i + 1 < t.Count && t[i] == ",") { values.Add(t[i + 1]); i += 2; }
                return values;
            }
            i++;
            while (i < t.Count && t[i] != "}")
            {
                if (t[i] != ",") values.Add(t[i]);
                i++;
            }
            i++;
            return values;
        }
        bool verdictSeen = false;
        while (i < t.Count)
        {
            var w = t[i++];
            switch (w)
            {
                case "ip" or "ip6":
                {
                    var field = t[i++];
                    if (field == "protocol" || field == "nexthdr") { r.Protocol = Proto(Values()[0]); break; }
                    if (field is not ("saddr" or "daddr")) throw new InvalidOperationException($"{w} {field}: only saddr, daddr and protocol are matched by Windows Firewall");
                    if (i < t.Count && t[i] == "!=") throw new InvalidOperationException("!=: Windows Firewall rules can't negate an address match");
                    var list = Values().Select(a => Address(a, w == "ip6")).ToList();
                    if (field == "saddr") r.SAddr.AddRange(list); else r.DAddr.AddRange(list);
                    r.Family = w;
                    break;
                }
                case "tcp" or "udp":
                {
                    r.Protocol = w;
                    if (i < t.Count && t[i] is "sport" or "dport")
                    {
                        var field = t[i++];
                        if (i < t.Count && t[i] == "!=") throw new InvalidOperationException("!=: Windows Firewall rules can't negate a port match");
                        var ports = Values().Select(Port).ToList();
                        if (field == "sport") r.SPort.AddRange(ports); else r.DPort.AddRange(ports);
                    }
                    else if (i < t.Count && t[i] == "flags") throw new InvalidOperationException("tcp flags: Windows Firewall doesn't match TCP flags");
                    break;
                }
                case "icmp" or "icmpv6":
                    r.Protocol = w;
                    if (i < t.Count && t[i] == "type") { i++; r.IcmpType = Values()[0]; }
                    break;
                case "meta":
                {
                    var key = t[i++];
                    if (key is "l4proto" or "protocol" or "nfproto") { var v = Values()[0]; if (key != "nfproto") r.Protocol = Proto(v); else r.Family = v == "ipv6" ? "ip6" : "ip"; }
                    else if (key is "iifname" or "oifname" or "iif" or "oif") throw new InvalidOperationException($"meta {key}: Windows Firewall rules select interface types (wireless, lan, ras), not named interfaces");
                    else throw new InvalidOperationException($"meta {key}: no Windows Firewall equivalent");
                    break;
                }
                case "iifname" or "oifname" or "iif" or "oif":
                    throw new InvalidOperationException($"{w}: Windows Firewall rules select interface types (wireless, lan, ras), not named interfaces");
                case "ct":
                    // Windows Firewall is stateful: replies to allowed traffic and established flows pass on their own.
                    i++;
                    Values();
                    break;
                case "counter":
                    if (i < t.Count && t[i] == "packets") i += 4;   // "counter packets N bytes M" from a saved ruleset
                    break;
                case "log" or "limit" or "notrack":
                    throw new InvalidOperationException($"{w}: no per-rule logging or rate limits in Windows Firewall (enable logging in its profile settings)");
                case "accept" or "drop" or "reject":
                    r.Verdict = w;
                    verdictSeen = true;
                    if (w == "reject" && i < t.Count && t[i] == "with") i += 3;
                    break;
                case "jump" or "goto" or "return" or "queue" or "continue":
                    throw new InvalidOperationException($"{w}: Windows Firewall has no user chains or queues");
                case "comment":
                    r.Comment = Values()[0].Trim('"');
                    break;
                default:
                    throw new UsageException($"syntax error, unexpected {w}");
            }
        }
        if (!verdictSeen) throw new InvalidOperationException("Windows Firewall rules need a verdict (accept, drop or reject)");
        if (r.Protocol == null && (r.SPort.Count > 0 || r.DPort.Count > 0)) throw new UsageException("ports need tcp or udp");
        return r;
    }

    static string Proto(string p) => p switch
    {
        "tcp" or "6" => "tcp",
        "udp" or "17" => "udp",
        "icmp" or "1" => "icmp",
        "icmpv6" or "ipv6-icmp" or "58" => "icmpv6",
        _ when int.TryParse(p, out _) => p,
        _ => throw new InvalidOperationException($"protocol {p}: give tcp, udp, icmp, ipv6-icmp or a number"),
    };

    static string Port(string p)
    {
        var parts = p.Split('-');
        if (parts.All(x => ushort.TryParse(x, out _)) && parts.Length <= 2) return p;
        throw new UsageException($"Could not resolve service: {p}");
    }

    static string Address(string a, bool v6)
    {
        var slash = a.IndexOf('/');
        var host = slash < 0 ? a : a[..slash];
        if (a.Contains('-') && a.Split('-').All(x => IPAddress.TryParse(x, out _))) return a;
        if (!IPAddress.TryParse(host, out var ip)) throw new UsageException($"Could not resolve hostname: {a}");
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 != v6)
            throw new UsageException($"{a}: address family doesn't match the {(v6 ? "ip6" : "ip")} match");
        return a;
    }

    // ---- Windows Firewall ----

    static List<Rule> ReadRules()
    {
        var rules = new List<Rule>();
        foreach (var path in new[] { PolicyRulesKey + @"\FirewallRules", RulesKey + @"\FirewallRules" })
        {
            using var key = Registry.LocalMachine.OpenSubKey(path);
            if (key == null) continue;
            foreach (var valueName in key.GetValueNames().Order(StringComparer.Ordinal))
                if (key.GetValue(valueName) is string data && FromRegistry(data) is { } rule)
                {
                    rule.RegistryName = valueName;
                    rules.Add(rule);
                }
        }
        // Handles: nft's rules first, then Windows' own; stable while the set of rules doesn't change.
        rules = rules.OrderBy(r => r.Ours ? 0 : 1).ThenBy(r => r.RegistryName, StringComparer.Ordinal).ToList();
        for (var k = 0; k < rules.Count; k++) rules[k].Handle = k + 2;
        return rules;
    }

    static Rule FromRegistry(string data)
    {
        var f = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in data.Split('|'))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0) continue;
            if (!f.TryGetValue(part[..eq], out var list)) f[part[..eq]] = list = new List<string>();
            list.Add(part[(eq + 1)..]);
        }
        string One(string k) => f.TryGetValue(k, out var l) ? l[0] : null;
        List<string> All(string k) => f.TryGetValue(k, out var l) ? l : new List<string>();
        if (!"TRUE".Equals(One("Active"), StringComparison.OrdinalIgnoreCase)) return null;
        var chain = One("Dir") switch { "In" => "input", "Out" => "output", _ => null };
        if (chain == null) return null;
        var name = Resolve(One("Name") ?? "");
        if (name.StartsWith(Prefix, StringComparison.Ordinal))
        {
            var text = name[Prefix.Length..];
            var space = text.IndexOf(' ');
            try
            {
                var own = ParseRule(text[..space], Tokenize(text[(space + 1)..]));
                own.Ours = true;
                return own;
            }
            catch (Exception e) when (e is UsageException or InvalidOperationException) { }
        }
        var rule = new Rule
        {
            Chain = chain,
            Comment = name,
            Verdict = One("Action") is "Block" ? "drop" : "accept",
            Protocol = One("Protocol") switch { null or "256" => null, "6" => "tcp", "17" => "udp", "1" => "icmp", "58" => "icmpv6", var p => p },
            App = One("App"),
            Service = One("Svc"),
        };
        var icmp = One("ICMP4") ?? One("ICMP6");
        if (icmp != null && rule.Protocol is "icmp" or "icmpv6" && icmp.Split(':')[0] is var type && type != "*")
            rule.IcmpType = IcmpName(rule.Protocol, type);
        var local = All("LA4").Concat(All("LA6")).Select(Cidr).ToList();
        var remote = All("RA4").Concat(All("RA6")).Select(Cidr).ToList();
        var lport = All("LPort").ToList();
        var rport = All("RPort").ToList();
        if (rule.Protocol is not ("tcp" or "udp")) { lport.Clear(); rport.Clear(); }
        if (chain == "input") { rule.SAddr = remote; rule.DAddr = local; rule.SPort = rport; rule.DPort = lport; }
        else { rule.SAddr = local; rule.DAddr = remote; rule.SPort = lport; rule.DPort = rport; }
        // Keyword addresses and ports (LocalSubnet, RPC, IPHTTPS ...) go in the comment; they aren't nft values.
        string Keywords(List<string> list, Func<string, bool> literal)
        {
            var keywords = list.Where(x => !literal(x)).ToList();
            list.RemoveAll(x => !literal(x));
            return keywords.Count > 0 ? string.Join(",", keywords) : null;
        }
        var extras = new[]
        {
            Keywords(rule.SAddr, IsAddress), Keywords(rule.DAddr, IsAddress), Keywords(rule.SPort, IsPort), Keywords(rule.DPort, IsPort),
        }.Where(x => x != null).ToList();
        if (extras.Count > 0) rule.Comment += " [" + string.Join("; ", extras) + "]";
        return rule;
    }

    static bool IsAddress(string a) => IPAddress.TryParse(a.Split('/')[0].Split('-')[0], out _);
    static bool IsPort(string p) => p.Split('-').All(x => ushort.TryParse(x, out _));

    static string Cidr(string a)
    {
        var slash = a.IndexOf('/');
        if (slash < 0 || !IPAddress.TryParse(a[(slash + 1)..], out var mask)) return a;
        var bits = mask.GetAddressBytes().Sum(b => System.Numerics.BitOperations.PopCount(b));
        return a[..slash] + "/" + bits;
    }

    static string IcmpName(string protocol, string type) => (protocol, type) switch
    {
        ("icmp", "0") => "echo-reply",
        ("icmp", "3") => "destination-unreachable",
        ("icmp", "8") => "echo-request",
        ("icmp", "11") => "time-exceeded",
        ("icmpv6", "1") => "destination-unreachable",
        ("icmpv6", "2") => "packet-too-big",
        ("icmpv6", "128") => "echo-request",
        ("icmpv6", "129") => "echo-reply",
        ("icmpv6", "133") => "nd-router-solicit",
        ("icmpv6", "134") => "nd-router-advert",
        ("icmpv6", "135") => "nd-neighbor-solicit",
        ("icmpv6", "136") => "nd-neighbor-advert",
        _ => type,
    };

    static string IcmpNumber(string protocol, string name)
    {
        foreach (var n in Enumerable.Range(0, 256).Select(x => x.ToString()))
            if (IcmpName(protocol, n) == name) return n;
        return int.TryParse(name, out _) ? name : throw new UsageException($"unknown {protocol} type {name}");
    }

    static readonly Dictionary<string, string> ResolvedNames = new();

    static unsafe string Resolve(string name)
    {
        if (!name.StartsWith('@')) return name;
        if (ResolvedNames.TryGetValue(name, out var cached)) return cached;
        var buffer = stackalloc char[1024];
        var result = SHLoadIndirectString(name, buffer, 1024, IntPtr.Zero) == 0 ? new string(buffer) : name;
        return ResolvedNames[name] = result;
    }

    static (bool inDrop, bool outDrop) Policy()
    {
        bool inDrop = false, outDrop = false;
        foreach (var (local, gpo) in new[] { ("DomainProfile", "DomainProfile"), ("StandardProfile", "PrivateProfile"), ("PublicProfile", "PublicProfile") })
        {
            using var lk = Registry.LocalMachine.OpenSubKey(RulesKey + @"\" + local);
            using var gk = Registry.LocalMachine.OpenSubKey(PolicyRulesKey + @"\" + gpo);
            int Get(string v, int fallback) => gk?.GetValue(v) as int? ?? lk?.GetValue(v) as int? ?? fallback;
            var enabled = Get("EnableFirewall", 1) != 0;
            inDrop |= enabled && Get("DefaultInboundAction", 1) != 0;
            outDrop |= enabled && Get("DefaultOutboundAction", 0) != 0;
        }
        return (inDrop, outDrop);
    }

    static int List(List<string> t, bool handles)
    {
        if (t.Count == 0) throw new UsageException("syntax error, unexpected end of file");
        string onlyChain = null;
        switch (t[0])
        {
            case "ruleset":
                if (t.Count > 1 && t[1] is not ("inet" or "ip" or "ip6")) return 0;
                break;
            case "tables":
                Console.WriteLine("table inet filter");
                return 0;
            case "table":
                Table(t.Skip(1).ToList());
                break;
            case "chain":
                onlyChain = RuleTarget(t.Skip(1).ToList()).Chain;
                break;
            case "chains":
                Console.WriteLine("table inet filter {\n\tchain input {\n\t\ttype filter hook input priority filter; policy " + (Policy().inDrop ? "drop" : "accept") + ";\n\t}\n\tchain output {\n\t\ttype filter hook output priority filter; policy " + (Policy().outDrop ? "drop" : "accept") + ";\n\t}\n}");
                return 0;
            case "sets" or "maps" or "flowtables" or "counters" or "quotas" or "limits" or "set" or "map":
                return 0;   // none exist
            default:
                throw new UsageException($"syntax error, unexpected {t[0]}");
        }
        var rules = ReadRules();
        var (inDrop, outDrop) = Policy();
        var sb = new StringBuilder();
        sb.Append("table inet filter {").Append(handles ? " # handle 1" : "").Append('\n');
        var first = true;
        foreach (var chain in new[] { "input", "output" })
        {
            if (onlyChain != null && chain != onlyChain) continue;
            if (!first) sb.Append('\n');
            first = false;
            sb.Append("\tchain ").Append(chain).Append(" {").Append(handles ? $" # handle {(chain == "input" ? 0 : 1)}" : "").Append('\n');
            sb.Append($"\t\ttype filter hook {chain} priority filter; policy {((chain == "input" ? inDrop : outDrop) ? "drop" : "accept")};\n");
            foreach (var r in rules.Where(r => r.Chain == chain))
                sb.Append("\t\t").Append(r.Text()).Append(handles ? $" # handle {r.Handle}" : "").Append('\n');
            sb.Append("\t}\n");
        }
        sb.Append("}\n");
        Console.Write(sb.ToString());
        return 0;
    }

    static void RequireAdmin()
    {
        if (!IsUserAnAdmin()) throw new InvalidOperationException("Could not process rule: Operation not permitted (run nft from an elevated prompt)");
    }

    static void AddRule(Rule r, List<string> body)
    {
        RequireAdmin();
        var text = r.Text();
        var name = Prefix + r.Chain + " " + text;
        if (name.Length > 1000) throw new InvalidOperationException("rule too long for a Windows Firewall rule name");
        var inbound = r.Chain == "input";
        var local = inbound ? r.DAddr : r.SAddr;
        var remote = inbound ? r.SAddr : r.DAddr;
        var localPorts = inbound ? r.DPort : r.SPort;
        var remotePorts = inbound ? r.SPort : r.DPort;
        var args = new List<string>
        {
            "advfirewall", "firewall", "add", "rule", "name=" + name, "dir=" + (inbound ? "in" : "out"),
            "action=" + (r.Verdict == "accept" ? "allow" : "block"), "enable=yes", "profile=any",
        };
        args.Add("protocol=" + r.Protocol switch
        {
            null => "any",
            "icmp" => "icmpv4" + (r.IcmpType != null ? ":" + IcmpNumber("icmp", r.IcmpType) + ",any" : ""),
            "icmpv6" => "icmpv6" + (r.IcmpType != null ? ":" + IcmpNumber("icmpv6", r.IcmpType) + ",any" : ""),
            var p => p,
        });
        if (local.Count > 0) args.Add("localip=" + string.Join(',', local));
        if (remote.Count > 0) args.Add("remoteip=" + string.Join(',', remote));
        else if (local.Count == 0 && r.Family != null) args.Add("remoteip=" + (r.Family == "ip6" ? "::-ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff" : "0.0.0.0-255.255.255.255"));
        if (localPorts.Count > 0) args.Add("localport=" + string.Join(',', localPorts));
        if (remotePorts.Count > 0) args.Add("remoteport=" + string.Join(',', remotePorts));
        if (r.Comment != null) args.Add("description=" + r.Comment);
        Netsh(args.ToArray());
    }

    static void DeleteRule(string chain, int handle, bool quiet)
    {
        RequireAdmin();
        var rule = ReadRules().FirstOrDefault(r => r.Handle == handle && r.Chain == chain);
        if (rule == null)
        {
            if (quiet) return;
            throw new InvalidOperationException($"Could not process rule: No such file or directory (no rule with handle {handle} in chain {chain})");
        }
        if (!rule.Ours) throw new InvalidOperationException($"handle {handle} is one of Windows' own rules (\"{rule.Comment}\"); nft only deletes rules it added");
        Netsh("advfirewall", "firewall", "delete", "rule", "name=" + Prefix + rule.Chain + " " + rule.Text(), "dir=" + (chain == "input" ? "in" : "out"));
    }

    static void Flush(string chain)
    {
        var ours = ReadRules().Where(r => r.Ours && (chain == null || r.Chain == chain)).ToList();
        if (ours.Count == 0) return;
        RequireAdmin();
        foreach (var name in ours.Select(r => (Prefix + r.Chain + " " + r.Text(), r.Chain)).Distinct())
            Netsh("advfirewall", "firewall", "delete", "rule", "name=" + name.Item1, "dir=" + (name.Item2 == "input" ? "in" : "out"));
    }

    static void SetPolicy(string chain, string policy)
    {
        RequireAdmin();
        foreach (var (local, gpo, netsh) in new[] { ("DomainProfile", "DomainProfile", "domainprofile"), ("StandardProfile", "PrivateProfile", "privateprofile"), ("PublicProfile", "PublicProfile", "publicprofile") })
        {
            using var lk = Registry.LocalMachine.OpenSubKey(RulesKey + @"\" + local);
            using var gk = Registry.LocalMachine.OpenSubKey(PolicyRulesKey + @"\" + gpo);
            int Get(string v, int fallback) => gk?.GetValue(v) as int? ?? lk?.GetValue(v) as int? ?? fallback;
            var inBlock = Get("DefaultInboundAction", 1) != 0;
            var outBlock = Get("DefaultOutboundAction", 0) != 0;
            if (chain == "input") inBlock = policy == "drop"; else outBlock = policy == "drop";
            Netsh("advfirewall", "set", netsh, "firewallpolicy", (inBlock ? "blockinbound" : "allowinbound") + "," + (outBlock ? "blockoutbound" : "allowoutbound"));
        }
    }

    static void Netsh(params string[] args)
    {
        var psi = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "netsh.exe"))
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0) throw new InvalidOperationException("netsh: " + output.Trim());
    }

    [DllImport("shell32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsUserAnAdmin();

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    static extern unsafe int SHLoadIndirectString(string source, char* output, uint size, IntPtr reserved);
}
