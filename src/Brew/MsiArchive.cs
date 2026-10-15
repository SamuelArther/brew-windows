using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Brew;

// Read-only MSI database/cabinet extraction: no installation, registry writes or custom actions.
internal static class MsiArchive
{
    public static async Task ExtractAsync(string file, string destination)
    {
        Check(MsiOpenDatabase(file, IntPtr.Zero, out var database));
        var work = destination + ".cabinets-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(work);
        try
        {
            var directories = Rows(database, "SELECT `Directory`,`Directory_Parent`,`DefaultDir` FROM `Directory`", 3)
                .ToDictionary(r => r[0]);
            var components = Rows(database, "SELECT `Component`,`Directory_` FROM `Component`", 2)
                .ToDictionary(r => r[0], r => r[1]);
            foreach (var media in Rows(database, "SELECT `Cabinet` FROM `Media`", 1))
            {
                if (!media[0].StartsWith('#')) throw new IOException("MSI requires an external cabinet.");
                var cabinet = Path.Combine(work, Guid.NewGuid().ToString("N") + ".cab");
                ReadStream(database, media[0][1..], cabinet);
                var start = new ProcessStartInfo("expand.exe")
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                foreach (var arg in new[] { "-F:*", cabinet, work }) start.ArgumentList.Add(arg);
                using var process = Process.Start(start);
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                if (process.ExitCode != 0) throw new IOException($"MSI cabinet extraction failed: {await error} {await output}");
                await Task.WhenAll(output, error);
                File.Delete(cabinet);
            }
            string DirectoryPath(string id, HashSet<string> seen = null)
            {
                seen ??= [];
                if (!seen.Add(id)) throw new IOException("Circular MSI directory.");
                var row = directories[id];
                var name = row[2].Split(':')[0].Split('|')[^1];
                if (string.IsNullOrEmpty(row[1]) || id is "TARGETDIR" or "ProgramFilesFolder" or "ProgramFiles64Folder") return "";
                return Path.Combine(DirectoryPath(row[1], seen), name == "." ? "" : name);
            }
            foreach (var row in Rows(database, "SELECT `File`,`Component_`,`FileName` FROM `File`", 3))
            {
                var relative = Path.Combine(DirectoryPath(components[row[1]]), row[2].Split('|')[^1]);
                var target = Path.GetFullPath(Path.Combine(destination, relative));
                if (!target.StartsWith(Path.GetFullPath(destination) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Unsafe path in MSI archive.");
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(Path.Combine(work, row[0]), target);
            }
        }
        finally { MsiCloseHandle(database); Directory.Delete(work, true); }
    }

    static List<string[]> Rows(uint database, string sql, int columns)
    {
        Check(MsiDatabaseOpenView(database, sql, out var view));
        try
        {
            Check(MsiViewExecute(view, 0));
            var rows = new List<string[]>();
            uint result;
            while ((result = MsiViewFetch(view, out var record)) == 0)
            {
                try
                {
                    var row = new string[columns];
                    for (uint i = 1; i <= columns; i++)
                    {
                        var text = new StringBuilder(32768); uint length = 32767;
                        Check(MsiRecordGetString(record, i, text, ref length));
                        row[i - 1] = text.ToString();
                    }
                    rows.Add(row);
                }
                finally { MsiCloseHandle(record); }
            }
            if (result != 259) Check(result);
            return rows;
        }
        finally { MsiCloseHandle(view); }
    }

    static void ReadStream(uint database, string name, string path)
    {
        Check(MsiDatabaseOpenView(database, "SELECT `Data` FROM `_Streams` WHERE `Name`='" + name.Replace("'", "''") + "'", out var view));
        try
        {
            Check(MsiViewExecute(view, 0));
            Check(MsiViewFetch(view, out var record));
            try
            {
                using var output = File.Create(path);
                var buffer = new byte[65536];
                while (true)
                {
                    uint count = (uint)buffer.Length;
                    Check(MsiRecordReadStream(record, 1, buffer, ref count));
                    if (count == 0) break;
                    output.Write(buffer, 0, (int)count);
                }
            }
            finally { MsiCloseHandle(record); }
        }
        finally { MsiCloseHandle(view); }
    }

    static void Check(uint error) { if (error != 0) throw new IOException($"MSI archive error {error}."); }
    [DllImport("msi.dll", CharSet = CharSet.Unicode, EntryPoint = "MsiOpenDatabaseW")] static extern uint MsiOpenDatabase(string file, IntPtr persistence, out uint database);
    [DllImport("msi.dll", CharSet = CharSet.Unicode, EntryPoint = "MsiDatabaseOpenViewW")] static extern uint MsiDatabaseOpenView(uint database, string sql, out uint view);
    [DllImport("msi.dll")] static extern uint MsiViewExecute(uint view, uint record);
    [DllImport("msi.dll")] static extern uint MsiViewFetch(uint view, out uint record);
    [DllImport("msi.dll", CharSet = CharSet.Unicode, EntryPoint = "MsiRecordGetStringW")] static extern uint MsiRecordGetString(uint record, uint field, StringBuilder value, ref uint length);
    [DllImport("msi.dll")] static extern uint MsiRecordReadStream(uint record, uint field, byte[] buffer, ref uint count);
    [DllImport("msi.dll")] static extern uint MsiCloseHandle(uint handle);
}
