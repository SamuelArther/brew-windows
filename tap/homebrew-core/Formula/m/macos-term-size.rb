class MacosTermSize < Formula
  desc "Get the terminal window size"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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
