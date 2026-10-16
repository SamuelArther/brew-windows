class Passt < Formula
  desc "User-mode networking daemon for virtual machines (QEMU stream socket)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "passt", "passt"
    windows_exe "libexec/wintools.exe", "pasta", "pasta"
  end

  def caveats
    <<~EOS
      passt serves a Unix socket QEMU connects to (-netdev stream,addr.type=unix); the guest's
      ARP, DHCP and DNS are answered and its TCP, UDP and ping traffic is carried on ordinary host
      sockets, with -t/-u port forwards into the guest. IPv4 only. pasta joins Linux network
      namespaces, which Windows doesn't have.
    EOS
  end

  test do
    assert_match "passt 2026", shell_output("#{bin}/passt --version")
    assert_match "--tcp-ports", shell_output("#{bin}/passt --help")
    assert_match "port ranges differ", shell_output("#{bin}/passt -t 80:90-91 2>&1", 2)
    assert_match "network namespaces", shell_output("#{bin}/pasta 2>&1", 1)
  end
end
