class Lsusb < Formula
  desc "List USB devices, just like the Linux lsusb command"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

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
