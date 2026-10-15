using System.Globalization;
using Brew.Formulae;

namespace Brew.Ruby;

public sealed record Dependency(string Name, IReadOnlyList<string> Tags)
{
    public bool Build => Tags.Contains("build");
    public bool Test => Tags.Contains("test");
    public bool Optional => Tags.Contains("optional");
    public bool Recommended => Tags.Contains("recommended");
    // Installed with the formula (what `brew deps` lists without --include-build/--include-test).
    public bool Runtime => !Build && !Test && !Optional;
}

public sealed record Conflict(string Name, string Because);

public sealed class LivecheckSpec
{
    public string Url;        // literal URL, or null when UrlSymbol is set
    public string UrlSymbol;  // stable / homepage / head / url
    public string Regex;      // Ruby regex source
    public string RegexFlags;
    public string Strategy;   // github_latest, page_match, git, ...
    public string Skip;       // non-null => skipped; empty string when no message
    public string Formula;    // `formula "other"` reuses another formula's livecheck
    internal RProc StrategyBlock;
}

// Everything known about a formula after evaluating its file.
public sealed class FormulaSpec
{
    public string Name, Path, ClassName;
    public string Desc, Homepage, Url, Sha256, Version, License, HeadUrl;
    public readonly List<string> Mirrors = new();
    public bool VersionFromUrl;   // no explicit `version`
    public int Revision, VersionScheme;
    public readonly List<Dependency> Dependencies = new();
    public readonly List<Dependency> UsesFromMacos = new();
    public readonly List<FormulaResource> Resources = new();
    public readonly List<Conflict> Conflicts = new();
    public readonly List<(string Name, string Description)> Options = new();
    public object KegOnlyReason;   // RSymbol or string
    public string KegOnlyExplanation;
    public bool Deprecated, Disabled;
    public DateOnly? DeprecationDate, DisableDate;
    public object DeprecationReason, DisableReason;   // RSymbol or string
    public string DeprecationReplacement, DisableReplacement;
    public LivecheckSpec Livecheck;
    public bool HasLivecheck => Livecheck != null;
    public bool HasCaveats;
    public bool HasTest => TestBlock != null;

    internal Interp Interp;
    internal RClass Class;
    internal RProc TestBlock;
    internal readonly List<RProc> PostInstall = new();

    // Homebrew's pkg_version: the Cellar directory name.
    public string PkgVersion => Revision > 0 ? $"{Version}_{Revision}" : Version;

    public bool KegOnly => KegOnlyReason != null;

    // KegOnlyReason#to_s
    public string KegOnlyText
    {
        get
        {
            if (!KegOnly) return null;
            if (!string.IsNullOrEmpty(KegOnlyExplanation)) return KegOnlyExplanation;
            return KegOnlyReason switch
            {
                RSymbol { Name: "versioned_formula" } => "this is an alternate version of another formula",
                RSymbol { Name: "provided_by_macos" } => "macOS already provides this software and installing another version in\nparallel can cause all kinds of trouble",
                RSymbol { Name: "shadowed_by_macos" } => "macOS provides similar software and installing this software in\nparallel can cause all kinds of trouble",
                RSymbol s => s.Name,
                _ => KegOnlyReason.ToString(),
            };
        }
    }

    static readonly Dictionary<string, string> Reasons = new()
    {
        ["does_not_build"] = "does not build",
        ["no_license"] = "has no license",
        ["repo_archived"] = "has an archived upstream repository",
        ["repo_removed"] = "has a removed upstream repository",
        ["unmaintained"] = "is not maintained upstream",
        ["unreachable"] = "is no longer reliably reachable upstream",
        ["unsupported"] = "is not supported upstream",
        ["deprecated_upstream"] = "is deprecated upstream",
        ["versioned_formula"] = "is a versioned formula",
        ["checksum_mismatch"] = "was built with an initially released source file that had a different checksum than the current one. " +
                                "Upstream's repository might have been compromised. We can re-package this once upstream has confirmed that they retagged their release",
    };

    // DeprecateDisable.message; null when neither deprecated nor disabled.
    public string DeprecationMessage(DateOnly? today = null)
    {
        if (!Deprecated && !Disabled) return null;
        var now = today ?? DateOnly.FromDateTime(DateTime.Today);
        string type = Deprecated ? "deprecated" : "disabled";
        var reasonObj = Deprecated ? DeprecationReason : DisableReason;
        string reason = reasonObj switch
        {
            RSymbol s => Reasons.GetValueOrDefault(s.Name, s.Name),
            null => null,
            _ => reasonObj.ToString(),
        };
        var msg = string.IsNullOrEmpty(reason) ? $"{type}!" : $"{type} because it {reason}!";
        var disableDate = DisableDate ?? DeprecationDate?.AddMonths(12);
        if (disableDate is { } d)
            msg += d < now ? $" It was disabled on {Iso(d)}." : $" It will be disabled on {Iso(d)}.";
        var replacement = Disabled ? DisableReplacement : DeprecationReplacement;
        if (!string.IsNullOrEmpty(replacement)) msg += $"\nReplacement:\n  brew install {replacement}\n";
        return msg;
    }

    static string Iso(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // Evaluates `def caveats` for an installed (or would-be) keg. Null when there are none.
    public string Caveats(string prefix, string brewPrefix)
    {
        if (!HasCaveats) return null;
        return FormulaLoader.WithInstance(this, new InstallContext(prefix, prefix, null, brewPrefix), (it, f) =>
        {
            var r = it.Invoke(f, "caveats", Array.Empty<object>(), null);
            var s = r == null ? null : it.ToS(r);
            return string.IsNullOrWhiteSpace(s) ? null : s;
        });
    }

    // Runs `test do`; throws InvalidOperationException with Homebrew's message on failure.
    public void RunTest(string prefix, string brewPrefix, string testPath) => FormulaLoader.RunTest(this, prefix, brewPrefix, testPath);

    // Runs `def post_install` if the formula has one.
    public void RunPostInstall(string prefix, string brewPrefix) => FormulaLoader.RunPostInstall(this, prefix, brewPrefix);

    public bool HasPostInstall;

    public FormulaDefinition ToDefinition() => Sha256 == null ? throw new InvalidOperationException($"{Name}: formula requires a sha256") : new()
    {
        Name = Name,
        Version = PkgVersion,
        Url = Url,
        Sha256 = Sha256,
        Description = Desc,
        Homepage = Homepage,
        License = License,
        Dependencies = Dependencies.Where(d => !d.Test && !d.Optional).Select(d => d.Name).ToList(),
        Resources = Resources.ToList(),
        Install = ctx => FormulaLoader.RunInstall(this, ctx),
    };
}
