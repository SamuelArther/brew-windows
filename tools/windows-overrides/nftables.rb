class Nftables < Formula
  desc "Netfilter tables userspace tools (on Windows Firewall)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "nft", "nft"
  end

  def caveats
    <<~EOS
      nft shows Windows Firewall as table inet filter (chain input = inbound rules, chain output =
      outbound rules). Changing rules needs an elevated (Administrator) prompt; rules added with nft
      are ordinary Windows Firewall rules named "nft: ...".
    EOS
  end

  test do
    assert_match "Windows Firewall", shell_output("#{bin}/nft --version")
    assert_match "table inet filter", shell_output("#{bin}/nft list tables")
    ruleset = shell_output("#{bin}/nft list ruleset")
    assert_match "type filter hook input priority filter; policy", ruleset
    assert_match "chain output", ruleset
    (testpath/"rules.nft").write <<~NFT
      flush ruleset
      table inet filter {
        chain input {
          type filter hook input priority 0; policy drop;
          ct state established,related accept
          tcp dport { 22, 80, 443 } accept
          ip saddr 10.0.0.0/8 udp dport 53 accept comment "dns"
          icmp type echo-request accept
        }
      }
    NFT
    system bin/"nft", "-c", "-f", testpath/"rules.nft"
    assert_match "forwarded traffic", shell_output("#{bin}/nft -c add rule inet filter forward accept 2>&1", 1)
    assert_match "no NAT", shell_output("#{bin}/nft -c add rule ip nat postrouting masquerade 2>&1", 1)
  end
end
