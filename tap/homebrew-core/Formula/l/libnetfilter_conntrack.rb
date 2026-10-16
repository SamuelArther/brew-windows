class LibnetfilterConntrack < Formula
  desc "Connection tracking tool on the Windows TCP connection table (conntrack)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "conntrack", "conntrack"
  end

  def caveats
    <<~EOS
      conntrack lists, counts and watches (-E, polled) the TCP connections Windows tracks, in
      conntrack's format; -D resets matching IPv4 connections (administrator). Windows keeps no UDP
      flows or NAT state. There is no libnetfilter_conntrack C library: programs use iphlpapi.
    EOS
  end

  test do
    system bin/"conntrack", "-L"
    assert_match(/^\d+$/, shell_output("#{bin}/conntrack -C").strip)
    assert_equal "0", shell_output("#{bin}/conntrack -C -p udp").strip
    assert_match "cpu=0", shell_output("#{bin}/conntrack -S")
    assert_match "conntrack v1.4.8", shell_output("#{bin}/conntrack --version")
    assert_match "NAT", shell_output("#{bin}/conntrack -L --src-nat 2>&1", 2)
  end
end
