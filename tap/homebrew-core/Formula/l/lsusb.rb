class Lsusb < Formula
  desc "List USB devices, just like the Linux lsusb command"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "lsusb", "lsusb"
  end

  test do
    # CI virtual machines may have no USB controller, so only require a clean run
    system bin/"lsusb"
    assert_match "lsusb", shell_output("#{bin}/lsusb --version")
    assert_match "vendor:[product]", shell_output("#{bin}/lsusb --help")
  end
end
