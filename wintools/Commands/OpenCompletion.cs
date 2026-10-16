using System.Runtime.CompilerServices;
using Microsoft.Win32;

namespace WinTools;

// open-completion: shell completion for `open` (and `open -a <application>`). Upstream is a bash
// completion file that lists the .app bundles in /Applications; here the application names are the
// Start menu shortcuts and the App Paths registrations, which is what Windows' "open with" knows. The
// script is printed rather than installed: eval "$(open-completion bash)" in ~/.bashrc, or
// open-completion powershell | Out-String | Invoke-Expression in the PowerShell profile.
static class OpenCompletion
{
    [ModuleInitializer]
    internal static void Register() => Commands.Add("open-completion", "Shell completion for open (bash and PowerShell)", Run);

    // open's options, used when `open -h` isn't there to ask.
    internal static readonly string[] Options =
    [
        "-a", "-b", "-e", "-t", "-f", "-F", "-W", "-R", "-n", "-g", "-j", "-h", "-s", "-u",
        "--args", "--env", "--stdin", "--stdout", "--stderr", "--fresh", "--background", "--hide",
        "--header", "--reveal", "--new", "--wait-apps",
    ];

    static int Run(string[] args)
    {
        var shell = "bash";
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: open-completion [bash|powershell]
                               open-completion --apps
                        Print a completion script for `open`, completing its options, file names and
                        application names after -a. Enable it with:
                          bash:        eval "$(open-completion bash)"
                          PowerShell:  open-completion powershell | Out-String | Invoke-Expression
                        --apps lists the application names -a completes.
                        """);
                    return 0;
                case "--apps":
                    Write(string.Join("\n", Applications()) + "\n");
                    return 0;
                case "bash" or "powershell" or "pwsh":
                    shell = arg;
                    break;
                default:
                    throw new UsageException($"unknown shell or option {arg} (see open-completion --help)");
            }
        }
        Write(shell == "bash" ? BashScript : PowerShellScript());
        return 0;
    }

    // Scripts go out with LF line endings: bash chokes on CRs.
    static void Write(string text)
    {
        using var stdout = Console.OpenStandardOutput();
        var bytes = System.Text.Encoding.UTF8.GetBytes(text.Replace("\r\n", "\n"));
        stdout.Write(bytes);
    }

    // Start menu shortcut names and App Paths executables, the applications Windows can open files with.
    internal static List<string> Applications()
    {
        var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in new[] { Environment.SpecialFolder.CommonStartMenu, Environment.SpecialFolder.StartMenu })
        {
            var programs = Path.Combine(Environment.GetFolderPath(folder), "Programs");
            if (!Directory.Exists(programs)) continue;
            try
            {
                foreach (var link in Directory.EnumerateFiles(programs, "*.lnk", SearchOption.AllDirectories))
                {
                    var name = Path.GetFileNameWithoutExtension(link);
                    if (!name.Contains("uninstall", StringComparison.OrdinalIgnoreCase)) names.Add(name);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }
        foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            using var key = hive.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths");
            if (key == null) continue;
            foreach (var exe in key.GetSubKeyNames())
                names.Add(Path.GetFileNameWithoutExtension(exe));
        }
        return [.. names];
    }

    const string BashScript = """
        # bash completion for open (brew-windows open-completion)
        _open() {
          COMPREPLY=()
          local cur="${COMP_WORDS[COMP_CWORD]}"
          local prev="${COMP_WORDS[COMP_CWORD-1]}"
          local line OLDIFS="$IFS"

          # Application names after -a
          if [[ "$prev" == -a* ]]; then
            local applications
            applications="$(open-completion --apps 2>/dev/null)"
            while IFS=$'\n' read -r line; do COMPREPLY+=("$line"); done < <(IFS=$'\n' compgen -W "$applications" -- "$cur")
            IFS="$OLDIFS"
            return
          fi

          case "$cur" in
            -*)
              local options
              options="$(open -h 2>&1 | grep -oE '(^|[[:space:]])--?[a-zA-Z][a-zA-Z-]*' | tr -d ' \t' | sort -u)"
              [[ -z "$options" ]] && options="-a -b -e -t -f -F -W -R -n -g -j -h -s -u --args --env --stdin --stdout --stderr --fresh --background --hide --header --reveal --new --wait-apps"
              while IFS=$'\n' read -r line; do COMPREPLY+=("$line"); done < <(compgen -W "$options" -- "$cur")
              ;;
            *)
              while IFS=$'\n' read -r line; do COMPREPLY+=("$line"); done < <(IFS=$'\n' compgen -o default -- "$cur")
              ;;
          esac
          IFS="$OLDIFS"
        }

        complete -o filenames -F _open open

        """;

    static string PowerShellScript() => $$"""
        # PowerShell completion for open (brew-windows open-completion)
        Register-ArgumentCompleter -Native -CommandName open, open.exe -ScriptBlock {
            param($wordToComplete, $commandAst, $cursorPosition)
            # Words ending before the cursor (the word being completed ends at it).
            $elements = @($commandAst.CommandElements | Where-Object { $_.Extent.EndOffset -lt $cursorPosition })
            $prev = if ($elements.Count -gt 0) { $elements[-1].Extent.Text } else { '' }
            if ($prev -like '-a*') {
                open-completion --apps | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
                    $text = if ($_ -match '\s') { "'" + $_ + "'" } else { $_ }
                    [System.Management.Automation.CompletionResult]::new($text, $_, 'ParameterValue', $_)
                }
                return
            }
            if ($wordToComplete -like '-*') {
                @({{string.Join(", ", Options.Select(o => "'" + o + "'"))}}) | Where-Object { $_ -clike "$wordToComplete*" } | ForEach-Object {
                    [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterName', $_)
                }
                return
            }
            $dir = Split-Path -Path $wordToComplete -Parent
            Get-ChildItem -Path "$wordToComplete*" -ErrorAction SilentlyContinue | ForEach-Object {
                $path = if ($dir) { Join-Path $dir $_.Name } else { $_.Name }
                $text = if ($path -match '\s') { "'" + $path + "'" } else { $path }
                [System.Management.Automation.CompletionResult]::new($text, $_.Name, 'ProviderItem', $_.FullName)
            }
        }

        """;
}
