class Blueutil < Formula
  desc "Get/set Bluetooth power and list, pair and connect devices"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "blueutil", "blueutil"
  end

  test do
    assert_match(/\d+\.\d+/, shell_output("#{bin}/blueutil --version"))
    assert_match "--paired", shell_output("#{bin}/blueutil --help")
    assert_match "discoverable", shell_output("#{bin}/blueutil --discoverable 1 2>&1", 1)
  end
end
