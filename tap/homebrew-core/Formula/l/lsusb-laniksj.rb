class LsusbLaniksj < Formula
  desc "List USB devices, just like the Linux lsusb command"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "lsusb", "lsusb"
  end

  test do
    # CI virtual machines may have no USB controller, so only require a clean run
    system bin/"lsusb"
    system bin/"lsusb", "-t"
    assert_match "lsusb", shell_output("#{bin}/lsusb --version")
  end
end
