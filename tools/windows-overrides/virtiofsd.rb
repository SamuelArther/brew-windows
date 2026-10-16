class Virtiofsd < Formula
  desc "Vhost-user virtio-fs device backend written in Rust"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "virtiofsd", "virtiofsd"
  end

  def caveats
    <<~EOS
      Windows hosts can't run vhost-user virtio-fs devices, so virtiofsd serves --shared-dir over
      9P2000.L instead, the protocol Linux's v9fs client speaks. For example:
        virtiofsd --shared-dir C:\vmshare --listen 0.0.0.0:564
      and in a Linux guest (Hyper-V, VirtualBox, VMware, QEMU):
        sudo mount -t 9p -o trans=tcp,port=564,version=9p2000.L,aname=/ HOST /mnt
      where HOST is the host as the guest sees it. The default --listen is 127.0.0.1:564, which
      guests behind VirtualBox NAT or QEMU user networking reach as 10.0.2.2; for other guests listen
      on an address they can reach, which may need a Windows Firewall rule. There's no
      authentication: anyone who can reach the address can use the share. --socket-path serves on a Windows
      AF_UNIX socket instead (trans=unix for a client or relay on this host).
      Ownership and permissions are synthesized: everything shows as owned by the mounting user, and
      chmod only toggles the read-only attribute. Guest symlinks are stored as WSL symlinks; mknod and
      xattrs aren't supported.
    EOS
  end

  test do
    assert_match "virtiofsd 1.14.0", shell_output("#{bin}/virtiofsd --version")
    assert_match '"type": "fs"', shell_output("#{bin}/virtiofsd --print-capabilities")
    # The 9P2000.L server against an in-process client over an in-memory stream pair: attach, walk,
    # lcreate, write, read, readdir, rename, unlink, links, setattr, --readonly, and no way out of the share.
    assert_match "self-test passed", shell_output("#{bin}/virtiofsd --self-test #{testpath}/selftest")
    assert_match "not a directory", shell_output("#{bin}/virtiofsd --shared-dir #{testpath}/nope 2>&1", 1)
    # On CI only: the same checks over real loopback TCP (a random port) and AF_UNIX listeners.
    if ENV["GITHUB_ACTIONS"]
      args = "--self-test #{testpath}/sockets --listen 127.0.0.1:0 --socket-path #{testpath}/9p.sock"
      assert_match "self-test passed", shell_output("#{bin}/virtiofsd #{args}")
    end
  end
end
