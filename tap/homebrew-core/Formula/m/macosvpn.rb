class Macosvpn < Formula
  desc "Create Windows VPN connections programmatically"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "macosvpn", "macosvpn"
  end

  test do
    assert_match(/\d+\.\d+/, shell_output("#{bin}/macosvpn version"))
    assert_match "Cisco", shell_output("#{bin}/macosvpn create --cisco Office 2>&1", 1)
    assert_match "sharedsecret", shell_output("#{bin}/macosvpn create --l2tp Office --endpoint vpn.example.com 2>&1", 2)
    assert_match "no VPN named", shell_output("#{bin}/macosvpn delete --name brew-no-such-vpn 2>&1", 1)
  end
end
