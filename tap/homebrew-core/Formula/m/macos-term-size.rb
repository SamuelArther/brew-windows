class MacosTermSize < Formula
  desc "Get the terminal window size"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "term-size", "term-size"
  end

  test do
    assert_match "columns", shell_output("#{bin}/term-size --help")
    assert_match(/\d+\s+\d+|no console attached/, shell_output("#{bin}/term-size 2>&1", nil))
  end
end
