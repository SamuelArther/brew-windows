using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace WinTools;

// iptables / ip6tables (+ -save / -restore) on Windows Firewall. The filter table's INPUT and OUTPUT chains
// are the inbound and outbound firewall rules, read straight from the registry (language-independent and
// fast); -A/-I/-D/-R/-F/-P change them through `netsh advfirewall`. Rules added here are named
// "iptables: <rule>" (or "ip6tables: ...") so they can be found again; Windows' own rules are listed but
// never deleted. Windows Firewall doesn't filter forwarded traffic, do NAT or keep per-rule counters, so
// FORWARD rules, other tables and user chains print a one-line explanation instead.
static class Iptables
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("iptables", "Administer IPv4 Windows Firewall rules with iptables syntax", a => Run("iptables", a, v6: false));
        Commands.Add("ip6tables", "Administer IPv6 Windows Firewall rules with ip6tables syntax", a => Run("ip6tables", a, v6: true));
        Commands.Add("iptables-save", "Print IPv4 Windows Firewall rules in iptables-save format", a => Save("iptables-save", a, v6: false));
        Commands.Add("ip6tables-save", "Print IPv6 Windows Firewall rules in ip6tables-save format", a => Save("ip6tables-save", a, v6: true));
        Commands.Add("iptables-restore", "Apply iptables-save rules to Windows Firewall", a => Restore("iptables-restore", a, v6: false));
        Commands.Add("ip6tables-restore", "Apply ip6tables-save rules to Windows Firewall", a => Restore("ip6tables-restore", a, v6: true));
    }

    const string RulesKey = @"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy";
    const string PolicyRulesKey = @"SOFTWARE\Policies\Microsoft\WindowsFirewall";
    static readonly string[] Chains = ["INPUT", "FORWARD", "OUTPUT"];

    const string Help = """
        Usage: iptables [-t filter] <command> [chain] [rule-specification] [options]

        Windows Firewall with iptables syntax: INPUT = inbound rules, OUTPUT = outbound rules.

        Commands:
          -A, --append  chain rule        add a rule (Windows Firewall rules are unordered, so -I is the same)
          -I, --insert  chain [num] rule
          -D, --delete  chain rule|num    delete a rule this tool added
          -R, --replace chain num rule
          -C, --check   chain rule        exit 0 if the rule exists
          -L, --list    [chain]           list rules (-n numeric, -v verbose, --line-numbers)
          -S, --list-rules [chain]        print rules as iptables commands
          -F, --flush   [chain]           delete every rule this tool added
          -Z, --zero    [chain]           accepted; Windows keeps no per-rule counters
          -P, --policy  chain ACCEPT|DROP set the default inbound/outbound action (all profiles)

        Rule specification:
          -p tcp|udp|icmp|all|<number>    -s/-d addr[/mask][,...] (also Windows keywords like LocalSubnet)
          --dport/--sport port[:port]     -m multiport --dports/--sports p1,p2,...
          -m iprange --src-range/--dst-range a-b    -m comment --comment text
          -m conntrack --ctstate ...      -j ACCEPT|DROP|REJECT (REJECT drops: Windows sends no reset)
        """;

    // ---- the rule model -------------------------------------------------------------------------------

    sealed class Rule
    {
        public string Chain, Protocol, IcmpType, Comment, Target, App, Service, Name;
        public List<string> Sources = [], Dests = [], SPorts = [], DPorts = [];
        public bool Ours, StateOnly;

        // Canonical `-A CHAIN ...` form, the same whether the rule came from the registry or the command line.
        public string Spec()
        {
            var sb = new StringBuilder("-A ").Append(Chain);
            if (Sources.Count > 0) sb.Append(" -s ").Append(string.Join(',', Sources));
            if (Dests.Count > 0) sb.Append(" -d ").Append(string.Join(',', Dests));
            if (Protocol != null) sb.Append(" -p ").Append(Protocol);
            if (IcmpType != null)
                sb.Append(Protocol == "ipv6-icmp" ? " -m icmp6 --icmpv6-type " : " -m icmp --icmp-type ").Append(IcmpType);
            if (SPorts.Count > 1 || DPorts.Count > 1)
            {
                sb.Append(" -m multiport");
                if (SPorts.Count > 0) sb.Append(" --sports ").Append(string.Join(',', SPorts));
                if (DPorts.Count > 0) sb.Append(" --dports ").Append(string.Join(',', DPorts));
            }
            else if (SPorts.Count + DPorts.Count > 0)
            {
                sb.Append(" -m ").Append(Protocol);
                if (SPorts.Count > 0) sb.Append(" --sport ").Append(SPorts[0]);
                if (DPorts.Count > 0) sb.Append(" --dport ").Append(DPorts[0]);
            }
            var comment = FullComment();
            if (comment != null) sb.Append(" -m comment --comment ").Append(Quote(comment));
            return sb.Append(" -j ").Append(Target).ToString();
        }

        // The comment carries what iptables can't express: the program or service a Windows rule is for.
        public string FullComment()
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Comment)) parts.Add(Comment);
            if (App != null) parts.Add("program=" + App);
            if (Service != null) parts.Add("service=" + Service);
            return parts.Count == 0 ? null : string.Join("; ", parts);
        }
    }

    static string Quote(string s) =>
        s.Length > 0 && s.All(c => c > ' ' && c != '"' && c != '\\') ? s : "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    // ---- iptables / ip6tables ------------------------------------------------------------------------

    static int Run(string name, string[] args, bool v6)
    {
        string table = "filter", command = null, chain = null, policy = null;
        int? number = null;
        bool numeric = false, verbose = false, lineNumbers = false;
        var ruleArgs = new List<string>();

        var expanded = new List<string>();
        foreach (var arg in args)
        {
            // Bundled short flags such as -nvL.
            if (arg.Length > 2 && arg[0] == '-' && arg[1] != '-' && arg[1..].All(c => "nvxLSFZ".Contains(c)))
                expanded.AddRange(arg[1..].Select(c => "-" + c));
            else expanded.Add(arg);
        }
        var a = expanded;
        string Next(ref int i, string opt) =>
            i + 1 < a.Count ? a[++i] : throw new UsageException($"option \"{opt}\" requires an argument");
        string OptionalChain(ref int i) => i + 1 < a.Count && !a[i + 1].StartsWith('-') ? a[++i] : null;
        void SetCommand(string c)
        {
            if (command != null) throw new UsageException($"Cannot use {c} with {command}");
            command = c;
        }

        for (var i = 0; i < a.Count; i++)
        {
            var arg = a[i];
            switch (arg)
            {
                case "-h" or "--help": Console.WriteLine(Help.Replace("iptables", name)); return 0;
                case "-V" or "--version": Console.WriteLine($"{name} v{Commands.Version} (Windows Firewall)"); return 0;
                case "-t" or "--table": table = Next(ref i, arg); break;
                case "-n" or "--numeric": numeric = true; break;
                case "-v" or "--verbose": verbose = true; break;
                case "-x" or "--exact": break;
                case "--line-numbers": lineNumbers = true; break;
                case "-w" or "--wait":
                    if (i + 1 < a.Count && a[i + 1].All(char.IsAsciiDigit)) i++;
                    break;
                case "-W" or "--wait-interval": Next(ref i, arg); break;
                case "-A" or "--append" or "-C" or "--check":
                    SetCommand(arg.TrimStart('-')[..1].ToUpperInvariant());
                    chain = Next(ref i, arg);
                    break;
                case "-I" or "--insert" or "-D" or "--delete" or "-R" or "--replace":
                    SetCommand(arg.TrimStart('-')[..1].ToUpperInvariant());
                    chain = Next(ref i, arg);
                    if (i + 1 < a.Count && int.TryParse(a[i + 1], out var n)) { number = n; i++; }
                    else if (command == "R") throw new UsageException("-R requires a rule number");
                    break;
                case "-L" or "--list" or "-S" or "--list-rules" or "-F" or "--flush" or "-Z" or "--zero":
                    SetCommand(arg is "--list-rules" ? "S" : arg.TrimStart('-')[..1].ToUpperInvariant());
                    chain = OptionalChain(ref i);
                    break;
                case "-P" or "--policy":
                    SetCommand("P");
                    chain = Next(ref i, arg);
                    policy = Next(ref i, arg);
                    break;
                case "-N" or "--new-chain" or "-X" or "--delete-chain" or "-E" or "--rename-chain":
                    throw new InvalidOperationException("user-defined chains aren't supported: Windows Firewall has only inbound (INPUT) and outbound (OUTPUT) rules");
                default: ruleArgs.Add(arg); break;
            }
        }
        if (table != "filter")
            throw new InvalidOperationException($"table '{table}' isn't available: Windows Firewall only filters" +
                (table == "nat" ? " (for port forwarding use `netsh interface portproxy`)" : ""));
        if (command == null) throw new UsageException($"no command specified (see {name} --help)");
        // `iptables -L -n INPUT`: the chain may come after the options.
        if (command is "L" or "S" or "Z" or "F" && chain == null && ruleArgs.Count == 1 && !ruleArgs[0].StartsWith('-'))
        {
            chain = ruleArgs[0];
            ruleArgs.Clear();
        }
        if (chain != null && !Chains.Contains(chain))
            throw new InvalidOperationException($"No chain/target/match by that name: '{chain}' (Windows Firewall has INPUT and OUTPUT)");
        if (command is "L" or "S" or "Z" or "F" && ruleArgs.Count > 0)
            throw new UsageException($"unexpected argument '{ruleArgs[0]}'");

        switch (command)
        {
            case "L": List(name, v6, chain, numeric, verbose, lineNumbers); return 0;
            case "S": ListRules(v6, chain); return 0;
            case "Z": return 0;
            case "C":
                var wanted = ParseRule(ruleArgs, chain, v6).Spec();
                if (ReadRules(v6).Any(r => r.Chain == chain && r.Spec() == wanted)) return 0;
                Console.Error.WriteLine($"{name}: Bad rule (does a matching rule exist in that chain?).");
                return 1;
        }

        // Everything below changes the firewall.
        if (!IsUserAnAdmin())
        {
            Console.Error.WriteLine($"{name}: Permission denied (run it from an elevated, Administrator prompt)");
            return 4;
        }
        var prefix = v6 ? "ip6tables: " : "iptables: ";
        switch (command)
        {
            case "P": SetPolicy(chain, policy); return 0;
            case "F":
                foreach (var r in ReadRules(v6).Where(r => r.Ours && (chain == null || r.Chain == chain)).Select(r => r.Name).Distinct())
                    Netsh("advfirewall", "firewall", "delete", "rule", "name=" + r);
                return 0;
        }
        if (chain == "FORWARD")
            throw new InvalidOperationException("Windows Firewall doesn't filter forwarded traffic, so the FORWARD chain can't hold rules");

        var existing = ReadRules(v6).Where(r => r.Chain == chain).ToList();
        if (command == "D" && number != null)
        {
            if (ruleArgs.Count > 0) throw new UsageException("-D takes a rule number or a rule, not both");
            DeleteNumber(existing, chain, number.Value);
            return 0;
        }

        var rule = ParseRule(ruleArgs, chain, v6);
        var spec = rule.Spec();
        switch (command)
        {
            case "D":
                var match = existing.FirstOrDefault(r => r.Spec() == spec && r.Ours);
                if (match == null)
                {
                    Console.Error.WriteLine(existing.Any(r => r.Spec() == spec)
                        ? $"{name}: that rule belongs to Windows; {name} only deletes rules it added (use wf.msc or netsh advfirewall)"
                        : $"{name}: Bad rule (does a matching rule exist in that chain?).");
                    return 1;
                }
                Netsh("advfirewall", "firewall", "delete", "rule", "name=" + match.Name);
                return 0;
            case "R":
                DeleteNumber(existing, chain, number.Value);
                break;
        }
        if (rule.StateOnly)
        {
            // Windows Firewall is stateful: replies to allowed connections always get through.
            Console.Error.WriteLine($"{name}: note: Windows Firewall always allows ESTABLISHED,RELATED traffic; no rule needed");
            return 0;
        }
        if (existing.Any(r => r.Ours && r.Spec() == spec)) return 0;
        Netsh(NetshAdd(rule, prefix + spec, v6));
        return 0;
    }

    static void DeleteNumber(List<Rule> rules, string chain, int number)
    {
        var ordered = Ordered(rules);
        if (number < 1 || number > ordered.Count) throw new InvalidOperationException($"Index of deletion too big: chain {chain} has {ordered.Count} rules");
        var rule = ordered[number - 1];
        if (!rule.Ours)
            throw new InvalidOperationException($"rule {number} of {chain} is the Windows rule \"{rule.Comment}\"; only rules added with iptables can be deleted (use wf.msc or netsh advfirewall)");
        Netsh("advfirewall", "firewall", "delete", "rule", "name=" + rule.Name);
    }

    // Our own rules first, so the numbers of rules the user added stay small and stable.
    static List<Rule> Ordered(IEnumerable<Rule> rules) => rules.OrderBy(r => r.Ours ? 0 : 1).ToList();

    static void List(string name, bool v6, string onlyChain, bool numeric, bool verbose, bool lineNumbers)
    {
        var rules = ReadRules(v6);
        var (inDrop, outDrop) = Policy();
        var any = v6 ? (numeric ? "::/0" : "anywhere") : (numeric ? "0.0.0.0/0" : "anywhere");
        var opt = v6 ? "" : "--";
        var first = true;
        foreach (var chain in Chains.Where(c => onlyChain == null || c == onlyChain))
        {
            if (!first) Console.WriteLine();
            first = false;
            var policy = chain == "INPUT" ? (inDrop ? "DROP" : "ACCEPT") : chain == "OUTPUT" ? (outDrop ? "DROP" : "ACCEPT") : "ACCEPT";
            Console.WriteLine(verbose ? $"Chain {chain} (policy {policy} 0 packets, 0 bytes)" : $"Chain {chain} (policy {policy})");
            var head = new StringBuilder();
            if (lineNumbers) head.Append("num  ");
            if (verbose) head.Append(" pkts bytes ");
            head.Append($"{"target",-10} {"prot",-4} {"opt",-3} ");
            if (verbose) head.Append($"{"in",-6} {"out",-6} ");
            head.Append($"{"source",-20} {"destination",-20}");
            Console.WriteLine(head.ToString().TrimEnd());
            var n = 0;
            foreach (var r in Ordered(rules.Where(r => r.Chain == chain)))
            {
                var line = new StringBuilder();
                if (lineNumbers) line.Append($"{++n,-4} ");
                if (verbose) line.Append($"{0,5} {0,5} ");
                line.Append($"{r.Target,-10} {r.Protocol ?? "all",-4} {opt,-3} ");
                if (verbose) line.Append($"{"*",-6} {"*",-6} ");
                string Addr(List<string> l) => l.Count == 0 ? any : string.Join(',', numeric ? l : l.Select(x => x.Replace("/32", "").Replace("/128", "")));
                line.Append($"{Addr(r.Sources),-20} {Addr(r.Dests),-20} ");
                if (r.IcmpType != null) line.Append(v6 ? $"ipv6-icmptype {r.IcmpType} " : $"icmptype {r.IcmpType} ");
                if (r.SPorts.Count > 1 || r.DPorts.Count > 1)
                {
                    line.Append("multiport ");
                    if (r.SPorts.Count > 0) line.Append($"sports {string.Join(',', r.SPorts)} ");
                    if (r.DPorts.Count > 0) line.Append($"dports {string.Join(',', r.DPorts)} ");
                }
                else
                {
                    if (r.SPorts.Count > 0) line.Append($"{r.Protocol} spt{(r.SPorts[0].Contains(':') ? "s" : "")}:{r.SPorts[0]} ");
                    if (r.DPorts.Count > 0) line.Append($"{r.Protocol} dpt{(r.DPorts[0].Contains(':') ? "s" : "")}:{r.DPorts[0]} ");
                }
                var comment = r.FullComment();
                if (comment != null) line.Append($"/* {comment} */");
                Console.WriteLine(line.ToString().TrimEnd());
            }
        }
    }

    static void ListRules(bool v6, string onlyChain)
    {
        var (inDrop, outDrop) = Policy();
        foreach (var chain in Chains.Where(c => onlyChain == null || c == onlyChain))
            Console.WriteLine($"-P {chain} {(chain == "INPUT" && inDrop || chain == "OUTPUT" && outDrop ? "DROP" : "ACCEPT")}");
        foreach (var r in Ordered(ReadRules(v6).Where(r => onlyChain == null || r.Chain == onlyChain)).OrderBy(r => r.Chain == "INPUT" ? 0 : 1))
            Console.WriteLine(r.Spec());
    }

    // ---- iptables-save / iptables-restore ------------------------------------------------------------

    static int Save(string name, string[] args, bool v6)
    {
        string file = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine($"Usage: {name} [-t filter] [-f file]\nPrint the Windows Firewall rules in {name} format.");
                    return 0;
                case "-V" or "--version": Console.WriteLine($"{name} v{Commands.Version} (Windows Firewall)"); return 0;
                case "-c" or "--counters": break;
                case "-t" or "--table":
                    if (++i >= args.Length) throw new UsageException("-t requires a table name");
                    if (args[i] != "filter") throw new InvalidOperationException($"table '{args[i]}' isn't available: Windows Firewall only filters");
                    break;
                case "-f" or "--file":
                    file = ++i < args.Length ? args[i] : throw new UsageException("-f requires a file name");
                    break;
                default: throw new UsageException($"unknown option {args[i]}");
            }
        }
        var (inDrop, outDrop) = Policy();
        var stamp = DateTime.Now.ToString("ddd MMM d HH:mm:ss yyyy");
        var sb = new StringBuilder();
        sb.Append($"# Generated by {name} v{Commands.Version} on {stamp}\n*filter\n");
        sb.Append($":INPUT {(inDrop ? "DROP" : "ACCEPT")} [0:0]\n:FORWARD ACCEPT [0:0]\n:OUTPUT {(outDrop ? "DROP" : "ACCEPT")} [0:0]\n");
        foreach (var r in Ordered(ReadRules(v6)).OrderBy(r => r.Chain == "INPUT" ? 0 : 1)) sb.Append(r.Spec()).Append('\n');
        sb.Append($"COMMIT\n# Completed on {stamp}\n");
        if (file != null) File.WriteAllText(file, sb.ToString());
        else Console.Write(sb.ToString());
        return 0;
    }

    // Flushes the rules iptables added (unless -n), then adds each saved rule that Windows doesn't already
    // have, so restoring a save made on this machine leaves Windows' own rules alone.
    static int Restore(string name, string[] args, bool v6)
    {
        bool noflush = false, test = false, verbose = false;
        string file = null;
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine($"Usage: {name} [-n] [-t] [-v] [file]\nApply rules in iptables-save format to Windows Firewall (stdin if no file).");
                    return 0;
                case "-V" or "--version": Console.WriteLine($"{name} v{Commands.Version} (Windows Firewall)"); return 0;
                case "-n" or "--noflush": noflush = true; break;
                case "-t" or "--test": test = true; break;
                case "-v" or "--verbose": verbose = true; break;
                case "-c" or "--counters" or "-w" or "--wait": break;
                default:
                    if (arg.StartsWith('-') && arg != "-") throw new UsageException($"unknown option {arg}");
                    file = arg;
                    break;
            }
        }
        var text = (file == null || file == "-" ? Console.In.ReadToEnd() : File.ReadAllText(file)).TrimStart((char)0xFEFF);
        var rules = new List<Rule>();
        var policies = new Dictionary<string, string>();
        string table = null;
        var lineNo = 0;
        foreach (var raw in text.Split('\n'))
        {
            lineNo++;
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            try
            {
                if (line.StartsWith('*')) table = line[1..];
                else if (table != "filter") throw new InvalidOperationException($"table '{table}' isn't available: Windows Firewall only filters");
                else if (line == "COMMIT") continue;
                else if (line.StartsWith(':'))
                {
                    var parts = line[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2) throw new UsageException("bad chain line");
                    if (!Chains.Contains(parts[0])) throw new InvalidOperationException($"user-defined chain '{parts[0]}' isn't supported");
                    if (parts[1] != "-") policies[parts[0]] = parts[1];
                }
                else
                {
                    var words = SplitWords(line);
                    if (words.Count < 2 || words[0] is not ("-A" or "--append" or "-I" or "--insert"))
                        throw new UsageException($"only -A/-I rules can be restored, not '{words[0]}'");
                    var chain = words[1];
                    if (!Chains.Contains(chain)) throw new InvalidOperationException($"No chain/target/match by that name: '{chain}'");
                    if (chain == "FORWARD") throw new InvalidOperationException("Windows Firewall doesn't filter forwarded traffic");
                    var skip = words[0] is "-I" or "--insert" && words.Count > 2 && int.TryParse(words[2], out _) ? 3 : 2;
                    rules.Add(ParseRule(words.Skip(skip).ToList(), chain, v6));
                }
            }
            catch (Exception e) when (e is UsageException or InvalidOperationException)
            {
                throw new InvalidOperationException($"line {lineNo} failed: {e.Message}");
            }
        }
        if (test) return 0;
        if (!IsUserAnAdmin())
        {
            Console.Error.WriteLine($"{name}: Permission denied (run it from an elevated, Administrator prompt)");
            return 4;
        }
        var prefix = v6 ? "ip6tables: " : "iptables: ";
        var current = ReadRules(v6);
        if (!noflush)
        {
            foreach (var n in current.Where(r => r.Ours).Select(r => r.Name).Distinct())
                Netsh("advfirewall", "firewall", "delete", "rule", "name=" + n);
            current.RemoveAll(r => r.Ours);
        }
        var have = current.Select(r => r.Spec()).ToHashSet();
        foreach (var (chain, target) in policies)
            if (chain != "FORWARD") SetPolicy(chain, target);
        foreach (var rule in rules.Where(r => !r.StateOnly))
        {
            var spec = rule.Spec();
            if (!have.Add(spec)) continue;
            if (verbose) Console.WriteLine(spec);
            Netsh(NetshAdd(rule, prefix + spec, v6));
        }
        return 0;
    }

    // Shell-style word splitting with double quotes and backslash escapes, as iptables-save writes them.
    static List<string> SplitWords(string line)
    {
        var words = new List<string>();
        var cur = new StringBuilder();
        bool quoted = false, inWord = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '\\' && i + 1 < line.Length && quoted) { cur.Append(line[++i]); continue; }
            if (c == '"') { quoted = !quoted; inWord = true; continue; }
            if (!quoted && char.IsWhiteSpace(c))
            {
                if (inWord) { words.Add(cur.ToString()); cur.Clear(); inWord = false; }
                continue;
            }
            cur.Append(c);
            inWord = true;
        }
        if (inWord) words.Add(cur.ToString());
        return words;
    }

    // ---- parsing a rule specification ----------------------------------------------------------------

    static readonly Dictionary<string, string> IcmpNames = new()
    {
        ["echo-reply"] = "0", ["destination-unreachable"] = "3", ["source-quench"] = "4", ["redirect"] = "5",
        ["echo-request"] = "8", ["router-advertisement"] = "9", ["router-solicitation"] = "10", ["time-exceeded"] = "11",
        ["parameter-problem"] = "12", ["timestamp-request"] = "13", ["timestamp-reply"] = "14",
    };
    static readonly Dictionary<string, string> Icmp6Names = new()
    {
        ["destination-unreachable"] = "1", ["packet-too-big"] = "2", ["time-exceeded"] = "3", ["parameter-problem"] = "4",
        ["echo-request"] = "128", ["echo-reply"] = "129", ["router-solicitation"] = "133", ["router-advertisement"] = "134",
        ["neighbour-solicitation"] = "135", ["neighbor-solicitation"] = "135", ["neighbour-advertisement"] = "136",
        ["neighbor-advertisement"] = "136", ["redirect"] = "137",
    };
    static readonly string[] AddressKeywords =
        ["LocalSubnet", "DNS", "DHCP", "WINS", "DefaultGateway", "Intranet", "RmtIntranet", "Internet", "Ply2Renders", "CaptivePortal"];

    static Rule ParseRule(List<string> words, string chain, bool v6)
    {
        var rule = new Rule { Chain = chain };
        var states = new List<string>();
        string Arg(ref int i, string opt) =>
            i + 1 < words.Count ? words[++i] : throw new UsageException($"option \"{opt}\" requires an argument");
        for (var i = 0; i < words.Count; i++)
        {
            var w = words[i];
            switch (w)
            {
                case "!": throw new InvalidOperationException("negated matches (!) aren't supported by Windows Firewall");
                case "-p" or "--protocol": rule.Protocol = Protocol(Arg(ref i, w), v6); break;
                case "-s" or "--source" or "--src": rule.Sources.AddRange(Addresses(Arg(ref i, w), v6)); break;
                case "-d" or "--destination" or "--dst": rule.Dests.AddRange(Addresses(Arg(ref i, w), v6)); break;
                case "--src-range": rule.Sources.Add(Range(Arg(ref i, w), v6)); break;
                case "--dst-range": rule.Dests.Add(Range(Arg(ref i, w), v6)); break;
                case "--dport" or "--destination-port" or "--dports" or "--destination-ports": rule.DPorts.AddRange(Ports(Arg(ref i, w))); break;
                case "--sport" or "--source-port" or "--sports" or "--source-ports": rule.SPorts.AddRange(Ports(Arg(ref i, w))); break;
                case "--ports": throw new InvalidOperationException("--ports (either direction) isn't supported; use --sports or --dports");
                case "--comment": rule.Comment = Arg(ref i, w); break;
                case "--icmp-type" or "--icmpv6-type":
                    var type = Arg(ref i, w);
                    var names = v6 ? Icmp6Names : IcmpNames;
                    rule.IcmpType = names.GetValueOrDefault(type) ?? (type.Split('/')[0].All(char.IsAsciiDigit) ? type.Split('/')[0]
                        : throw new UsageException($"unknown ICMP type '{type}'"));
                    break;
                case "--state" or "--ctstate": states.AddRange(Arg(ref i, w).Split(',')); break;
                case "-m" or "--match":
                    var m = Arg(ref i, w);
                    if (m is not ("tcp" or "udp" or "icmp" or "icmp6" or "icmpv6" or "multiport" or "comment" or "iprange" or "state" or "conntrack"))
                        throw new InvalidOperationException($"match '{m}' isn't supported by Windows Firewall");
                    break;
                case "-i" or "--in-interface" or "-o" or "--out-interface":
                    throw new InvalidOperationException("matching by interface isn't supported by Windows Firewall rules");
                case "-j" or "--jump":
                    var t = Arg(ref i, w);
                    rule.Target = t is "ACCEPT" or "DROP" or "REJECT" ? t
                        : throw new InvalidOperationException($"target '{t}' isn't supported: Windows Firewall can only allow (ACCEPT) or block (DROP/REJECT)");
                    break;
                case "-g" or "--goto": throw new InvalidOperationException("user-defined chains aren't supported");
                case "--reject-with": Arg(ref i, w); break;
                default: throw new UsageException($"Bad argument `{w}'");
            }
        }
        if (rule.Target == null) throw new InvalidOperationException("a rule needs a target (-j ACCEPT, DROP or REJECT)");
        // "Name; program=...; service=..." comments (as -S prints Windows' rules) carry the rule's program and service.
        if (rule.Comment != null)
        {
            var parts = rule.Comment.Split("; ").ToList();
            rule.App = parts.FirstOrDefault(x => x.StartsWith("program="))?[8..];
            rule.Service = parts.FirstOrDefault(x => x.StartsWith("service="))?[8..];
            parts.RemoveAll(x => x.StartsWith("program=") || x.StartsWith("service="));
            rule.Comment = parts.Count > 0 ? string.Join("; ", parts) : null;
        }
        if (rule.Target == "REJECT") rule.Target = "DROP";
        if ((rule.SPorts.Count > 0 || rule.DPorts.Count > 0) && rule.Protocol is not ("tcp" or "udp"))
            throw new UsageException("port matches need -p tcp or -p udp");
        if (rule.IcmpType != null && rule.Protocol is not ("icmp" or "ipv6-icmp"))
            throw new UsageException("--icmp-type needs -p icmp");
        if (states.Count > 0)
        {
            var upper = states.Select(s => s.ToUpperInvariant()).ToList();
            if (upper.Contains("INVALID") || upper.Contains("UNTRACKED"))
                throw new InvalidOperationException("Windows Firewall can't match INVALID or UNTRACKED connections");
            // NEW is what firewall rules match anyway; ESTABLISHED/RELATED replies are always let through.
            rule.StateOnly = !upper.Contains("NEW") && rule.Target == "ACCEPT";
        }
        return rule;
    }

    static string Protocol(string p, bool v6)
    {
        p = p.ToLowerInvariant();
        return p switch
        {
            "all" or "0" => null,
            "tcp" or "6" => "tcp",
            "udp" or "17" => "udp",
            "icmp" or "1" => v6 ? throw new UsageException("use -p ipv6-icmp with ip6tables") : "icmp",
            "ipv6-icmp" or "icmpv6" or "icmp6" or "58" => v6 ? "ipv6-icmp" : throw new UsageException("-p ipv6-icmp needs ip6tables"),
            _ when int.TryParse(p, out var n) && n is > 0 and < 256 => p,
            _ => throw new UsageException($"unknown protocol \"{p}\" specified"),
        };
    }

    static IEnumerable<string> Ports(string spec) =>
        spec.Split(',').Select(p =>
        {
            // Windows port keywords such as RPC, RPC-EPMap, IPHTTPS or Teredo pass through as they are.
            if (p.Length > 0 && char.IsAsciiLetter(p[0]) && p.All(c => char.IsAsciiLetterOrDigit(c) || c == '-')) return p;
            var parts = p.Split(':', '-');
            if (parts.Length > 2 || parts.Any(x => !ushort.TryParse(x, out _))) throw new UsageException($"invalid port/service `{p}' specified");
            return parts.Length == 2 ? parts[0] + ":" + parts[1] : parts[0];
        });

    static IEnumerable<string> Addresses(string spec, bool v6)
    {
        foreach (var item in spec.Split(','))
        {
            var keyword = AddressKeywords.FirstOrDefault(k => k.Equals(item, StringComparison.OrdinalIgnoreCase));
            if (keyword != null) { yield return keyword; continue; }
            if (item.Contains('-') && !item.Contains('/')) { yield return Range(item, v6); continue; }
            var slash = item.IndexOf('/');
            var host = slash < 0 ? item : item[..slash];
            if (!IPAddress.TryParse(host, out var ip))
            {
                // A host name: iptables resolves it once, when the rule is added.
                IPAddress[] found;
                try { found = Dns.GetHostAddresses(host); }
                catch (SocketException) { throw new UsageException($"host/network `{item}' not found"); }
                foreach (var f in found.Where(f => f.AddressFamily == Family(v6)))
                    yield return f + (v6 ? "/128" : "/32");
                continue;
            }
            if (ip.AddressFamily != Family(v6)) throw new UsageException($"host/network `{item}' not found");
            var bits = v6 ? 128 : 32;
            var prefix = bits;
            if (slash >= 0)
            {
                var m = item[(slash + 1)..];
                if (IPAddress.TryParse(m, out var mask) && !v6) prefix = MaskBits(mask);
                else if (!int.TryParse(m, out prefix) || prefix < 0 || prefix > bits) throw new UsageException($"invalid mask `{m}' specified");
            }
            if (prefix == 0) continue;   // 0.0.0.0/0 is "anywhere"
            yield return Network(ip, prefix) + "/" + prefix;
        }
    }

    static string Range(string spec, bool v6)
    {
        var parts = spec.Split('-');
        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var lo) || !IPAddress.TryParse(parts[1], out var hi)
            || lo.AddressFamily != Family(v6) || hi.AddressFamily != Family(v6))
            throw new UsageException($"invalid address range `{spec}'");
        return lo + "-" + hi;
    }

    static AddressFamily Family(bool v6) => v6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;

    static int MaskBits(IPAddress mask)
    {
        var bits = 0;
        foreach (var b in mask.GetAddressBytes()) bits += System.Numerics.BitOperations.PopCount(b);
        return bits;
    }

    static IPAddress Network(IPAddress ip, int prefix)
    {
        var bytes = ip.GetAddressBytes();
        for (var i = 0; i < bytes.Length; i++)
        {
            var keep = Math.Clamp(prefix - i * 8, 0, 8);
            bytes[i] &= (byte)(0xFF << (8 - keep));
        }
        return new IPAddress(bytes);
    }

    // The netsh command that creates a rule. A rule with no addresses gets the whole address family as its
    // remote range, so an iptables rule doesn't also open IPv6 (and vice versa).
    static string[] NetshAdd(Rule rule, string ruleName, bool v6)
    {
        var inbound = rule.Chain == "INPUT";
        var local = inbound ? rule.Dests : rule.Sources;
        var remote = inbound ? rule.Sources : rule.Dests;
        var localPorts = inbound ? rule.DPorts : rule.SPorts;
        var remotePorts = inbound ? rule.SPorts : rule.DPorts;
        var args = new List<string>
        {
            "advfirewall", "firewall", "add", "rule", "name=" + ruleName, "dir=" + (inbound ? "in" : "out"),
            "action=" + (rule.Target == "ACCEPT" ? "allow" : "block"), "enable=yes", "profile=any",
        };
        var protocol = rule.Protocol switch
        {
            null => "any",
            "icmp" => "icmpv4" + (rule.IcmpType != null ? ":" + rule.IcmpType + ",any" : ""),
            "ipv6-icmp" => "icmpv6" + (rule.IcmpType != null ? ":" + rule.IcmpType + ",any" : ""),
            var p => p,
        };
        args.Add("protocol=" + protocol);
        if (local.Count > 0) args.Add("localip=" + string.Join(',', local));
        if (remote.Count > 0) args.Add("remoteip=" + string.Join(',', remote));
        else if (local.Count == 0) args.Add("remoteip=" + (v6 ? "::-ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff" : "0.0.0.0-255.255.255.255"));
        if (localPorts.Count > 0) args.Add("localport=" + string.Join(',', localPorts).Replace(':', '-'));
        if (remotePorts.Count > 0) args.Add("remoteport=" + string.Join(',', remotePorts).Replace(':', '-'));
        if (rule.Comment != null)
        {
            if (rule.Comment.Contains('"')) throw new UsageException("comments can't contain double quotes");
            args.Add("description=" + rule.Comment);
        }
        if (rule.App != null) args.Add("program=" + rule.App);
        if (rule.Service != null) args.Add("service=" + rule.Service);
        return [.. args];
    }

    // ---- reading Windows Firewall from the registry --------------------------------------------------

    static List<Rule> ReadRules(bool v6)
    {
        var rules = new List<Rule>();
        foreach (var path in new[] { PolicyRulesKey + @"\FirewallRules", RulesKey + @"\FirewallRules" })
        {
            using var key = Registry.LocalMachine.OpenSubKey(path);
            if (key == null) continue;
            foreach (var valueName in key.GetValueNames())
                if (key.GetValue(valueName) is string data && FromRegistry(data, v6) is { } rule) rules.Add(rule);
        }
        return rules;
    }

    // One registry rule, e.g. "v2.33|Action=Allow|Active=TRUE|Dir=In|Protocol=6|LPort=80|Name=...|".
    static Rule FromRegistry(string data, bool v6)
    {
        var f = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in data.Split('|'))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0) continue;
            var k = part[..eq];
            if (!f.TryGetValue(k, out var list)) f[k] = list = [];
            list.Add(part[(eq + 1)..]);
        }
        string One(string k) => f.TryGetValue(k, out var l) ? l[0] : null;
        if (!"TRUE".Equals(One("Active"), StringComparison.OrdinalIgnoreCase)) return null;
        var chain = One("Dir") switch { "In" => "INPUT", "Out" => "OUTPUT", _ => null };
        if (chain == null) return null;

        var name = Resolve(One("Name") ?? "");
        foreach (var prefix in new[] { "iptables: ", "ip6tables: " })
        {
            if (!name.StartsWith(prefix, StringComparison.Ordinal)) continue;
            if (prefix.StartsWith("ip6") != v6) return null;
            var words = SplitWords(name[prefix.Length..]);
            try
            {
                var own = ParseRule(words.Skip(2).ToList(), chain, v6);
                own.Ours = true;
                own.Name = name;
                return own;
            }
            catch (Exception e) when (e is UsageException or InvalidOperationException) { break; }
        }

        List<string> All(string prefix) =>
            f.Where(kv => kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).SelectMany(kv => kv.Value).ToList();
        var v4Addr = All("LA4").Concat(All("RA4")).Any();
        var v6Addr = All("LA6").Concat(All("RA6")).Any();
        if ((v4Addr || v6Addr) && (v6 ? !v6Addr : !v4Addr)) return null;
        var proto = One("Protocol");
        if (proto == "1" && v6 || proto == "58" && !v6) return null;

        var rule = new Rule
        {
            Chain = chain,
            Name = name,
            Comment = name,
            Target = One("Action") is "Block" ? "DROP" : "ACCEPT",
            Protocol = proto switch { null => null, "6" => "tcp", "17" => "udp", "1" => "icmp", "58" => "ipv6-icmp", "256" => null, var p => p },
            App = One("App"),
            Service = One("Svc"),
        };
        var icmp = One(v6 ? "ICMP6" : "ICMP4");
        if (icmp != null && rule.Protocol != null && rule.Protocol.Contains("icmp")) rule.IcmpType = icmp.Split(':')[0] is var t && t != "*" ? t : null;
        var la = All(v6 ? "LA6" : "LA4").Select(a => Address(a, v6)).ToList();
        var ra = All(v6 ? "RA6" : "RA4").Select(a => Address(a, v6)).ToList();
        var lp = All("LPort").Select(PortRange).ToList();
        var rp = All("RPort").Select(PortRange).ToList();
        if (rule.Protocol is not ("tcp" or "udp")) { lp.Clear(); rp.Clear(); }
        if (chain == "INPUT") { rule.Sources = ra; rule.Dests = la; rule.SPorts = rp; rule.DPorts = lp; }
        else { rule.Sources = la; rule.Dests = ra; rule.SPorts = lp; rule.DPorts = rp; }
        return rule;
    }

    // Registry port ranges are "5000-5020"; keywords such as RPC-EPMap keep their dash.
    static string PortRange(string p) => p.Length > 0 && char.IsAsciiDigit(p[0]) ? p.Replace('-', ':') : p;

    // Registry addresses: "1.2.3.4", "10.0.0.0/255.0.0.0", "a-b" or a keyword such as LocalSubnet.
    static string Address(string a, bool v6)
    {
        var slash = a.IndexOf('/');
        var host = slash < 0 ? a : a[..slash];
        if (!IPAddress.TryParse(host, out var ip)) return a;
        if (a.Contains('-')) return a;
        var prefix = v6 ? 128 : 32;
        if (slash >= 0)
        {
            var m = a[(slash + 1)..];
            if (IPAddress.TryParse(m, out var mask)) prefix = MaskBits(mask);
            else int.TryParse(m, out prefix);
        }
        return Network(ip, prefix) + "/" + prefix;
    }

    static readonly Dictionary<string, string> ResolvedNames = new();

    // "@FirewallAPI.dll,-28502" and "@{package?ms-resource://...}" names are resource references.
    static unsafe string Resolve(string name)
    {
        if (!name.StartsWith('@')) return name;
        if (ResolvedNames.TryGetValue(name, out var cached)) return cached;
        var buffer = stackalloc char[1024];
        var result = SHLoadIndirectString(name, buffer, 1024, IntPtr.Zero) == 0 ? new string(buffer) : name;
        return ResolvedNames[name] = result;
    }

    // Default actions: true = block. A profile with the firewall off allows everything. When profiles
    // differ, the strictest one is reported.
    static (bool inDrop, bool outDrop) Policy()
    {
        bool inDrop = false, outDrop = false;
        foreach (var (local, gpo) in new[] { ("DomainProfile", "DomainProfile"), ("StandardProfile", "PrivateProfile"), ("PublicProfile", "PublicProfile") })
        {
            var (enabled, inBlock, outBlock) = ProfilePolicy(local, gpo);
            inDrop |= enabled && inBlock;
            outDrop |= enabled && outBlock;
        }
        return (inDrop, outDrop);
    }

    static (bool enabled, bool inBlock, bool outBlock) ProfilePolicy(string local, string gpo)
    {
        using var lk = Registry.LocalMachine.OpenSubKey(RulesKey + @"\" + local);
        using var gk = Registry.LocalMachine.OpenSubKey(PolicyRulesKey + @"\" + gpo);
        int Get(string v, int fallback) => gk?.GetValue(v) as int? ?? lk?.GetValue(v) as int? ?? fallback;
        return (Get("EnableFirewall", 1) != 0, Get("DefaultInboundAction", 1) != 0, Get("DefaultOutboundAction", 0) != 0);
    }

    // -P: set one direction's default action on every profile, keeping each profile's other direction.
    static void SetPolicy(string chain, string target)
    {
        if (target is not ("ACCEPT" or "DROP")) throw new InvalidOperationException($"Bad policy name '{target}' (use ACCEPT or DROP)");
        if (chain == "FORWARD")
        {
            if (target == "DROP") throw new InvalidOperationException("Windows Firewall doesn't filter forwarded traffic, so FORWARD can only be ACCEPT");
            return;
        }
        foreach (var (local, gpo, netsh) in new[] { ("DomainProfile", "DomainProfile", "domainprofile"), ("StandardProfile", "PrivateProfile", "privateprofile"), ("PublicProfile", "PublicProfile", "publicprofile") })
        {
            var (enabled, inBlock, outBlock) = ProfilePolicy(local, gpo);
            if (chain == "INPUT") inBlock = target == "DROP"; else outBlock = target == "DROP";
            Netsh("advfirewall", "set", netsh, "firewallpolicy",
                (inBlock ? "blockinbound" : "allowinbound") + "," + (outBlock ? "blockoutbound" : "allowoutbound"));
            // A DROP policy means nothing while that profile's firewall is switched off.
            if (!enabled && target == "DROP") Netsh("advfirewall", "set", netsh, "state", "on");
        }
    }

    static void Netsh(params string[] args)
    {
        var psi = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "netsh.exe"))
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi);
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
