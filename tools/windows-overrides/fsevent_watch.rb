class FseventWatch < Formula
  desc "File system events client with FSEvents-style output"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "fsevent_watch", "fsevent_watch"
  end

  test do
    system bin/"fsevent_watch", "--version"
    assert_match "--file-events", shell_output("#{bin}/fsevent_watch --help")
    assert_match "unknown format", shell_output("#{bin}/fsevent_watch --format=bogus 2>&1", 2)
  end
end
