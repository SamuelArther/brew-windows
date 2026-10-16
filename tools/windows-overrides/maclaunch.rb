class Maclaunch < Formula
  desc "Manage Windows startup items"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "maclaunch", "maclaunch"
  end

  test do
    system bin/"maclaunch", "list"
    assert_match "startup items", shell_output("#{bin}/maclaunch --help")
    assert_match "no startup item named", shell_output("#{bin}/maclaunch disable no-such-item-brew-test 2>&1", 1)
  end
end
