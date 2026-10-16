class MacosTermSize < Formula
  desc "Get the terminal window size"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
