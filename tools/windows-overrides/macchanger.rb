class Macchanger < Formula
  desc "Change a network adapter's MAC address"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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
