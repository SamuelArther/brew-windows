class Brightness < Formula
  desc "Change display brightness from the command-line"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "brightness", "brightness"
  end

  test do
    assert_match "usage: brightness", shell_output("#{bin}/brightness --help")
    assert_match "between 0 and 1", shell_output("#{bin}/brightness 2 2>&1", 2)
  end
end
