class WlClipboard < Formula
  desc "Command-line copy/paste utilities for the Windows clipboard"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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
