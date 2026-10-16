class Dockutil < Formula
  desc "Tool for managing taskbar (Dock) items"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
