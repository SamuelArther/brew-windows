class OpenCompletion < Formula
  desc "Bash and PowerShell completion for open"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "open-completion", "open-completion"
  end

  def caveats
    <<~EOS
      Enable the completion with:
        bash:        eval "$(open-completion bash)"
        PowerShell:  open-completion powershell | Out-String | Invoke-Expression
    EOS
  end

  test do
    assert_match "complete -o filenames -F _open open", shell_output("#{bin}/open-completion bash")
    assert_match "Register-ArgumentCompleter", shell_output("#{bin}/open-completion powershell")
    system bin/"open-completion", "--apps"
  end
end
