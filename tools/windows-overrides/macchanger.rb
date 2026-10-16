class Macchanger < Formula
  desc "Change a network adapter's MAC address"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
