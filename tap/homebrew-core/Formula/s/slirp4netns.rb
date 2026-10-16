class Slirp4netns < Formula
  desc "User-mode networking (slirp) for a virtual machine's frame socket"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "slirp4netns", "slirp4netns"
  end

  def caveats
    <<~EOS
      Windows has no network namespaces or TAP devices, so slirp4netns attaches to a QEMU
      -netdev stream server socket instead: slirp4netns --target-type=stream PATH. The guest gets
      10.0.2.100 by DHCP (gateway 10.0.2.2 reaches the host, 10.0.2.3 is DNS), and --api-socket
      accepts add_hostfwd/list_hostfwd/remove_hostfwd. IPv4 only.
    EOS
  end

  test do
    assert_match "slirp4netns version 1.3.6", shell_output("#{bin}/slirp4netns --version")
    assert_match "--target-type", shell_output("#{bin}/slirp4netns --help")
    assert_match "--target-type=stream", shell_output("#{bin}/slirp4netns 1234 tap0 2>&1", 1)
    shell_output("#{bin}/slirp4netns --target-type=stream #{testpath}/no-such.socket 2>&1", 1)
  end
end
