class TerminalNotifier < Formula
  desc "Send Windows toast notifications from the command-line"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "terminal-notifier", "terminal-notifier"
  end

  test do
    assert_match "terminal-notifier", shell_output("#{bin}/terminal-notifier -help")
    assert_match(/terminal-notifier \d+\.\d+/, shell_output("#{bin}/terminal-notifier -version"))
    assert_match "unknown option", shell_output("#{bin}/terminal-notifier -bogus 2>&1", 2)
  end
end
