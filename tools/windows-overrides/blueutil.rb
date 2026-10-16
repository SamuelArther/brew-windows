class Blueutil < Formula
  desc "Get/set Bluetooth power and list, pair and connect devices"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
