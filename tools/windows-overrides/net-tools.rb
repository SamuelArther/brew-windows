class NetTools < Formula
  desc "Linux networking base tools (ifconfig, netstat, route, arp) for Windows"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "ifconfig", "ifconfig"
    windows_exe "libexec/wintools.exe", "netstat", "netstat"
    windows_exe "libexec/wintools.exe", "route", "route"
    windows_exe "libexec/wintools.exe", "arp", "arp"
  end

  test do
    assert_match "Kernel Interface table", shell_output("#{bin}/netstat -i")
    assert_match "Kernel IP routing table", shell_output("#{bin}/route -n")
    assert_match "Active Internet connections", shell_output("#{bin}/netstat -tan")
    assert_match "inet 127.0.0.1  netmask 255.0.0.0", shell_output("#{bin}/ifconfig -a")
    assert_match "HWaddress", shell_output("#{bin}/arp -n")
  end
end
