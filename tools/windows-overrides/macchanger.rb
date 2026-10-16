class Macchanger < Formula
  desc "Change a network adapter's MAC address"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "macchanger", "macchanger"
  end

  test do
    assert_match "macchanger", shell_output("#{bin}/macchanger --version")
    assert_match "Please specify an interface name", shell_output("#{bin}/macchanger --show 2>&1", 1)
    assert_match "no such interface", shell_output("#{bin}/macchanger -s no-such-adapter 2>&1", 1)
    assert_match "invalid MAC address", shell_output("#{bin}/macchanger -m 12:34 no-such-adapter 2>&1", 2)
  end
end
