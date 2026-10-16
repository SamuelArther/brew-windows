class NetTools < Formula
  desc "Linux networking base tools (ifconfig, netstat, route, arp) for Windows"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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
