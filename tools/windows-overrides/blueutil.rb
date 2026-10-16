class Blueutil < Formula
  desc "Get/set Bluetooth power and list, pair and connect devices"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "blueutil", "blueutil"
  end

  test do
    assert_match(/\d+\.\d+/, shell_output("#{bin}/blueutil --version"))
    assert_match "--paired", shell_output("#{bin}/blueutil --help")
    assert_match "discoverable", shell_output("#{bin}/blueutil --discoverable 1 2>&1", 1)
  end
end
