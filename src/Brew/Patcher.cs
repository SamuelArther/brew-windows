using System.Text;
using System.Text.RegularExpressions;

namespace Brew;

// Applies unified diffs the way `patch -f -pN` does (GNU patch is not part of Windows):
// exact placement first, then nearby offsets, then up to two lines of context fuzz.
// Every target must stay inside the staged source tree. Line endings of patched files are kept.
// Like GNU patch it works on bytes: text is handled as Latin-1 (one char per byte), so a file in
// any encoding (Latin-1 sources, a UTF-8 BOM) keeps every byte the hunks don't change.
public static class Patcher
{
    static readonly Encoding Bytes = Encoding.Latin1;

    sealed record Hunk(int OldStart, List<string> Old, List<string> New, bool OldNoEol, bool NewNoEol);
    sealed record FilePatch(string OldName, string NewName, List<Hunk> Hunks);

    public static void Apply(string diff, int strip, string baseDirectory) => Apply(Encoding.UTF8.GetBytes(diff), strip, baseDirectory);

    public static void Apply(byte[] diff, int strip, string baseDirectory)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(baseDirectory));
        var files = Parse(Bytes.GetString(diff));
        if (files.Count == 0) throw new InvalidOperationException("Unsupported patch format: no target paths to verify.");
        foreach (var file in files)
        {
            var deleting = file.NewName == "/dev/null";
            var creating = file.OldName == "/dev/null";
            var name = Target(deleting ? file.OldName : file.NewName, strip);
            var path = Path.GetFullPath(Path.Combine(root, name));
            if (Path.IsPathRooted(name) || name.Split('/').Contains("..") || !path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Patch target path escapes the staged source tree: {name}");
            // GNU patch falls back to the old name when the new one does not exist.
            if (!creating && !File.Exists(path) && file.OldName != "/dev/null")
            {
                var alternate = Path.GetFullPath(Path.Combine(root, Target(file.OldName, strip)));
                if (File.Exists(alternate) && alternate.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) path = alternate;
            }
            Console.WriteLine($"patching file {Path.GetRelativePath(root, path).Replace('\\', '/')}");
            ApplyFile(file, path, creating, deleting);
        }
    }

    // Names arrive as bytes (Latin-1 chars); file names are UTF-8.
    static string Target(string name, int strip)
    {
        name = Encoding.UTF8.GetString(Bytes.GetBytes(name)).Replace('\\', '/');
        var parts = name.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= strip) throw new InvalidOperationException($"Cannot strip {strip} components from {name}");
        return string.Join('/', parts.Skip(strip));
    }

    static void ApplyFile(FilePatch file, string path, bool creating, bool deleting)
    {
        var existed = File.Exists(path);
        // Like GNU patch, a missing file whose hunks only add lines ("--- a/lib/utf8mac.h 1970-01-01 ...",
        // "@@ -0,0 +1,1608 @@": the epoch date some diffs give a new file) is created (libiconv's utf8mac patch).
        if (!existed && file.Hunks.Count > 0 && file.Hunks.All(h => h.Old.Count == 0)) creating = true;
        if (!existed && !creating) throw new InvalidOperationException($"can't find file to patch: {path}");
        var text = existed ? Bytes.GetString(File.ReadAllBytes(path)) : "";
        var crlf = text.Contains("\r\n");
        var endsWithNewline = text.Length == 0 || text.EndsWith('\n');
        var lines = text.Length == 0 ? new List<string>() : text.Replace("\r\n", "\n").Split('\n').ToList();
        if (lines.Count > 0 && lines[^1] == "" && endsWithNewline) lines.RemoveAt(lines.Count - 1);

        var offset = 0;
        var number = 0;
        foreach (var hunk in file.Hunks)
        {
            number++;
            var expected = Math.Max(0, hunk.OldStart - 1 + offset);
            var (at, fuzz) = Locate(lines, hunk, expected);
            if (at < 0) throw new InvalidOperationException($"Hunk #{number} FAILED at {hunk.OldStart}.");
            var oldLines = hunk.Old.Skip(fuzz).Take(hunk.Old.Count - 2 * fuzz).ToList();
            var newLines = hunk.New.Skip(fuzz).Take(hunk.New.Count - 2 * fuzz).ToList();
            var drift = at - Math.Min(expected + fuzz, lines.Count);
            lines.RemoveRange(at, oldLines.Count);
            lines.InsertRange(at, newLines);
            if (drift != 0 || fuzz > 0)
                Console.WriteLine($"Hunk #{number} succeeded at {at + 1 - fuzz}{(fuzz > 0 ? $" with fuzz {fuzz}" : "")}{(drift != 0 ? $" (offset {drift} line{(Math.Abs(drift) == 1 ? "" : "s")})" : "")}.");
            offset += newLines.Count - oldLines.Count + drift;
            if (hunk.NewNoEol && at + newLines.Count == lines.Count) endsWithNewline = false;
            else if (hunk.OldNoEol && !hunk.NewNoEol && at + newLines.Count == lines.Count) endsWithNewline = true;
        }

        if (deleting && lines.Count == 0) { File.Delete(path); return; }
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var eol = crlf ? "\r\n" : "\n";
        var output = string.Join(eol, lines) + (lines.Count > 0 && endsWithNewline ? eol : "");
        File.WriteAllBytes(path, Bytes.GetBytes(output));
    }

    // Exact match at or near the expected line, then GNU-style fuzz (ignore outer context lines).
    static (int At, int Fuzz) Locate(List<string> lines, Hunk hunk, int expected)
    {
        for (var fuzz = 0; fuzz <= 2; fuzz++)
        {
            if (fuzz > 0 && (hunk.Old.Count <= 2 * fuzz || !IsContext(hunk, fuzz))) break;
            var probe = hunk.Old.Skip(fuzz).Take(hunk.Old.Count - 2 * fuzz).ToList();
            var start = Math.Min(Math.Max(0, expected + fuzz), lines.Count);
            for (var distance = 0; distance <= lines.Count; distance++)
            {
                foreach (var candidate in distance == 0 ? [start] : new[] { start - distance, start + distance })
                    if (candidate >= 0 && candidate + probe.Count <= lines.Count && Matches(lines, candidate, probe))
                        return (candidate, fuzz);
                if (start - distance < 0 && start + distance + probe.Count > lines.Count) break;
            }
        }
        return (-1, 0);
    }

    // Fuzz may only drop lines that are context on both sides of the hunk.
    static bool IsContext(Hunk hunk, int fuzz) =>
        hunk.Old.Take(fuzz).SequenceEqual(hunk.New.Take(fuzz)) && hunk.Old.TakeLast(fuzz).SequenceEqual(hunk.New.TakeLast(fuzz));

    static bool Matches(List<string> lines, int at, List<string> probe)
    {
        for (var i = 0; i < probe.Count; i++)
            if (lines[at + i].TrimEnd('\r') != probe[i].TrimEnd('\r')) return false;
        return true;
    }

    static readonly Regex HunkHeader = new(@"^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@", RegexOptions.Compiled);

    static List<FilePatch> Parse(string diff)
    {
        var lines = diff.Replace("\r\n", "\n").Split('\n');
        var files = new List<FilePatch>();
        for (var i = 0; i < lines.Length; i++)
        {
            if (!lines[i].StartsWith("--- ") || i + 1 >= lines.Length || !lines[i + 1].StartsWith("+++ ")) continue;
            var file = new FilePatch(Name(lines[i][4..]), Name(lines[i + 1][4..]), []);
            i += 2;
            while (i < lines.Length)
            {
                var header = HunkHeader.Match(lines[i]);
                if (!header.Success) break;
                var oldCount = header.Groups[2].Success ? int.Parse(header.Groups[2].Value) : 1;
                var newCount = header.Groups[4].Success ? int.Parse(header.Groups[4].Value) : 1;
                List<string> old = [], @new = [];
                bool oldNoEol = false, newNoEol = false;
                char last = ' ';
                i++;
                while (i < lines.Length && (old.Count < oldCount || @new.Count < newCount || lines[i].StartsWith('\\')))
                {
                    var line = lines[i];
                    if (line.StartsWith('\\')) { if (last is '-' or ' ') oldNoEol = true; if (last is '+' or ' ') newNoEol = true; i++; continue; }
                    // Like GNU patch, a context line whose leading space got eaten (it starts with a tab, as in
                    // md5deep's config.guess patch) is still context, tab included.
                    var tag = line.Length == 0 || line[0] == '\t' ? ' ' : line[0];
                    var body = line.Length == 0 ? "" : line[0] == '\t' ? line : line[1..];
                    switch (tag)
                    {
                        case ' ': old.Add(body); @new.Add(body); break;
                        case '-': old.Add(body); break;
                        case '+': @new.Add(body); break;
                        default: throw new InvalidOperationException($"Malformed patch at line {i + 1}: {line}");
                    }
                    last = tag;
                    i++;
                }
                file.Hunks.Add(new Hunk(int.Parse(header.Groups[1].Value) == 0 ? 1 : int.Parse(header.Groups[1].Value), old, @new, oldNoEol, newNoEol));
            }
            i--;
            files.Add(file);
        }
        return files;
    }

    // "a/foo.c\t2024-01-01 ..." -> "a/foo.c"
    static readonly Regex Timestamp = new(@"\s+(\d{4}-\d{2}-\d{2}\s+\d{1,2}:\d{2}(:\d{2}(\.\d+)?)?(\s*[+-]\d{4})?|[A-Z][a-z]{2}\s+[A-Z][a-z]{2}\s+\d{1,2}\s+\d{1,2}:\d{2}:\d{2}\s+\d{4})$", RegexOptions.Compiled);

    static string Name(string field)
    {
        var name = field.Split('\t')[0].TrimEnd();
        if (name.StartsWith('"') && name.EndsWith('"')) name = Regex.Unescape(name[1..^1]);
        // Without a tab, GNU patch still drops a trailing timestamp ("kccommon.h  2013-11-08 09:27:47.000000000 -0500").
        else name = Timestamp.Replace(name, "");
        return name;
    }
}
