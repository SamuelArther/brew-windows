class Iproute2 < Formula
  desc "Linux routing utilities (ip, ss, bridge) for Windows networking"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

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
