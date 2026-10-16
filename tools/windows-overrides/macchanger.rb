class Macchanger < Formula
  desc "Change a network adapter's MAC address"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "macchanger", "macchanger"
  end

  test do
    assert_match "macchanger", shell_output("#{bin}/macchanger --version")
    assert_match "Please specify an interface name", shell_output("#{bin}/macchanger --show 2>&1", 1)
    assert_match "no such interface", shell_output("#{bin}/macchanger -s no-such-adapter 2>&1", 1)
    assert_match "invalid MAC address", shell_output("#{bin}/macchanger -m 12:34 no-such-adapter 2>&1", 2)
  end
end
