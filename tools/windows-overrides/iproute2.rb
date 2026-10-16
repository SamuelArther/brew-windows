class Iproute2 < Formula
  desc "Linux routing utilities (ip, ss, bridge) for Windows networking"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "ip", "ip"
    windows_exe "libexec/wintools.exe", "ss", "ss"
    windows_exe "libexec/wintools.exe", "bridge", "bridge"
  end

  test do
    output = shell_output("#{bin}/ip addr").strip
    assert_match "link/loopback 00:00:00:00:00:00 brd 00:00:00:00:00:00", output
    assert_match "inet 127.0.0.1/8", output
    system bin/"ip", "route"
    system bin/"ip", "neigh"
    assert_match "Usage: ip", shell_output("#{bin}/ip -help")
    assert_match "Local Address:Port", shell_output("#{bin}/ss -tln")
    assert_match "bridge utility", shell_output("#{bin}/bridge -V")
    assert_match "does not exist", shell_output("#{bin}/ip link show nosuch0 2>&1", 1)
  end
end
