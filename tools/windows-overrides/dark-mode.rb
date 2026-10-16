class DarkMode < Formula
  desc "Control Windows dark mode from the command-line"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "dark-mode", "dark-mode"
  end

  test do
    assert_match(/\A(on|off)\z/, shell_output("#{bin}/dark-mode status").strip)
    assert_match "Toggle dark mode", shell_output("#{bin}/dark-mode --help")
  end
end
