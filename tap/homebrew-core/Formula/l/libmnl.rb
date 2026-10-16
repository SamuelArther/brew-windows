class Libmnl < Formula
  desc "Rtnetlink-style link, address, route and neighbour dumps on IP Helper"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "rtnl-link-dump", "rtnl-link-dump"
    windows_exe "libexec/wintools.exe", "rtnl-addr-dump", "rtnl-addr-dump"
    windows_exe "libexec/wintools.exe", "rtnl-route-dump", "rtnl-route-dump"
    windows_exe "libexec/wintools.exe", "rtnl-neigh-dump", "rtnl-neigh-dump"
    windows_exe "libexec/wintools.exe", "rtnl-link-event", "rtnl-link-event"
  end

  def caveats
    <<~EOS
      These are libmnl's rtnetlink example tools, reading Windows' IP Helper tables (the counterpart
      of rtnetlink) in their output format. There is no libmnl C library or netlink socket on
      Windows: programs use iphlpapi.
    EOS
  end

  test do
    assert_match "index=", shell_output("#{bin}/rtnl-link-dump")
    assert_match "addr=127.0.0.1/8", shell_output("#{bin}/rtnl-addr-dump inet")
    assert_match "dst=127.0.0.0/8", shell_output("#{bin}/rtnl-route-dump inet")
    system bin/"rtnl-neigh-dump", "inet"
    shell_output("#{bin}/rtnl-route-dump ipx 2>&1", 2)
  end
end
