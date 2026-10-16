class Ddcutil < Formula
  desc "Control monitor settings using DDC/CI"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

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
