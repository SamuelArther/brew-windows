class WlClipboard < Formula
  desc "Command-line copy/paste utilities for the Windows clipboard"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "wl-copy", "wl-copy"
    windows_exe "libexec/wintools.exe", "wl-paste", "wl-paste"
  end

  test do
    assert_match "wl-copy", shell_output("#{bin}/wl-copy --version")
    assert_match "--list-types", shell_output("#{bin}/wl-paste --help")
    assert_match "no primary selection", shell_output("#{bin}/wl-paste --primary 2>&1", 1)
  end
end
