class Libnftnl < Formula
  desc "Windows Firewall shown as an nftables ruleset (nft-*-get)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "nft-ruleset-get", "nft-ruleset-get"
    windows_exe "libexec/wintools.exe", "nft-table-get", "nft-table-get"
    windows_exe "libexec/wintools.exe", "nft-chain-get", "nft-chain-get"
    windows_exe "libexec/wintools.exe", "nft-rule-get", "nft-rule-get"
  end

  def caveats
    <<~EOS
      The nft-*-get tools print Windows Firewall as nftables: table inet windows_firewall with
      input/output chains (policies from the profiles' default actions) and the enabled rules.
      Rules matching programs or services keep those as meta program/service. There is no libnftnl
      C library: Windows programs use the Windows Filtering Platform.
    EOS
  end

  test do
    assert_match "table inet windows_firewall {", shell_output("#{bin}/nft-ruleset-get")
    assert_match "type filter hook input", shell_output("#{bin}/nft-chain-get")
    assert_match "\"nftables\"", shell_output("#{bin}/nft-rule-get json")
    assert_match "table inet windows_firewall", shell_output("#{bin}/nft-table-get")
    shell_output("#{bin}/nft-rule-get bogus 2>&1", 2)
  end
end
