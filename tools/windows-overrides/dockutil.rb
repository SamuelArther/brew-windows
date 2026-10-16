class Dockutil < Formula
  desc "Tool for managing taskbar (Dock) items"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

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
