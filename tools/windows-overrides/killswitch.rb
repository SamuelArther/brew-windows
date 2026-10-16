class Killswitch < Formula
  desc "VPN kill switch using Windows Firewall block rules"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "killswitch", "killswitch"
  end

  test do
    assert_match "No VPN interface found", shell_output("#{bin}/killswitch 2>&1")
    assert_match "Interface", shell_output("#{bin}/killswitch")
    assert_match "No VPN interface found", shell_output("#{bin}/killswitch -p 2>&1", 1)
    assert_match(/\d+\.\d+/, shell_output("#{bin}/killswitch -v"))
  end
end
