class Blueutil < Formula
  desc "Get/set Bluetooth power and list, pair and connect devices"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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
