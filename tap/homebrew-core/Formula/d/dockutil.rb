class Dockutil < Formula
  desc "Tool for managing taskbar (Dock) items"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "dockutil", "dockutil"
  end

  test do
    assert_match(/\d+\.\d+/, shell_output("#{bin}/dockutil --version"))
    system bin/"dockutil", "--list"
    assert_match "was not found", shell_output("#{bin}/dockutil --find NoSuchApp", 1)
  end
end
