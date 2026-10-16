class Brightness < Formula
  desc "Change display brightness from the command-line"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

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
