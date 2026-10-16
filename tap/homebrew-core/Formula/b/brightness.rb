class Brightness < Formula
  desc "Change display brightness from the command-line"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
