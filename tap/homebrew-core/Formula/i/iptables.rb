class Iptables < Formula
  desc "Administer Windows Firewall rules with iptables syntax"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "iptables", "iptables"
    windows_exe "libexec/wintools.exe", "ip6tables", "ip6tables"
    windows_exe "libexec/wintools.exe", "iptables-save", "iptables-save"
    windows_exe "libexec/wintools.exe", "ip6tables-save", "ip6tables-save"
    windows_exe "libexec/wintools.exe", "iptables-restore", "iptables-restore"
    windows_exe "libexec/wintools.exe", "ip6tables-restore", "ip6tables-restore"
  end

  def caveats
    <<~EOS
      Rules added with iptables are ordinary (persistent) Windows Firewall rules named "iptables: ...".
      Changing rules needs an elevated (Administrator) prompt.
    EOS
  end

  test do
    assert_match "Windows Firewall", shell_output("#{bin}/iptables --version")
    assert_match "-P INPUT", shell_output("#{bin}/iptables -S")
    assert_match "Chain OUTPUT", shell_output("#{bin}/ip6tables -L -n")
    assert_match "*filter", shell_output("#{bin}/iptables-save")
    (testpath/"rules").write "*filter\n:INPUT DROP [0:0]\n-A INPUT -s 10.0.0.0/8 -p tcp -m multiport --dports 22,80 -j ACCEPT\nCOMMIT\n"
    system bin/"iptables-restore", "--test", testpath/"rules"
    assert_match "only filters", shell_output("#{bin}/iptables -t nat -L 2>&1", 1)
    assert_match "Bad rule", shell_output("#{bin}/iptables -C INPUT -p tcp --dport 1 -m comment --comment brewtest -j DROP 2>&1", 1)
  end
end
