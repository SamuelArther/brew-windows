class SocketVmnet < Formula
  desc "Daemon to provide vmnet-style networking for rootless QEMU"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "socket_vmnet", "socket_vmnet"
  end

  test do
    assert_match "socket_vmnet version", shell_output("#{bin}/socket_vmnet --version")
    # The switch and gateway (ARP, DHCP, ICMP) on synthetic frames, without sockets.
    assert_match "ok: DHCP OFFER", shell_output("#{bin}/socket_vmnet --self-test")
    # Binding the UNIX socket over an existing file fails like upstream's /dev/null test.
    (testpath/"taken").write ""
    assert_match "bind: Address already in use", shell_output("#{bin}/socket_vmnet #{testpath}/taken 2>&1", 1)
  end
end
