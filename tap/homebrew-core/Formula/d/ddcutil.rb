class Ddcutil < Formula
  desc "Control monitor settings using DDC/CI"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "ddcutil", "ddcutil"
  end

  test do
    assert_match "The following tests probe the runtime environment using multiple overlapping methods.",
                 shell_output("#{bin}/ddcutil environment")
    assert_match "VCP code 10: Brightness", shell_output("#{bin}/ddcutil vcpinfo 10")
    assert_match(/\d+\.\d+/, shell_output("#{bin}/ddcutil --version"))
  end
end
