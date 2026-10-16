class Libnl < Formula
  desc "Netlink Library Suite tools on the Windows IP Helper API"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "nl-link-list", "nl-link-list"
    windows_exe "libexec/wintools.exe", "nl-addr-list", "nl-addr-list"
    windows_exe "libexec/wintools.exe", "nl-route-list", "nl-route-list"
    windows_exe "libexec/wintools.exe", "nl-neigh-list", "nl-neigh-list"
    windows_exe "libexec/wintools.exe", "nl-monitor", "nl-monitor"
  end

  def caveats
    <<~EOS
      The tools read links, addresses, routes and neighbours from the IP Helper API, Windows'
      counterpart of rtnetlink. There is no libnl C library: Windows programs use iphlpapi.
    EOS
  end

  test do
    assert_match "inet 127.0.0.1", shell_output("#{bin}/nl-route-list")
    assert_match "loopback", shell_output("#{bin}/nl-link-list")
    assert_match "127.0.0.1/8 inet dev", shell_output("#{bin}/nl-addr-list --family=inet")
    shell_output("#{bin}/nl-neigh-list")
    shell_output("#{bin}/nl-route-list --no-such-option 2>&1", 2)
  end
end
