class Screenresolution < Formula
  desc "Get, set, and list display resolution"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "screenresolution", "screenresolution"
  end

  test do
    assert_match "screenresolution get", shell_output("#{bin}/screenresolution --help")
    assert_match(/version \d+\.\d+/, shell_output("#{bin}/screenresolution -version"))
    assert_match "bad mode", shell_output("#{bin}/screenresolution set 12x 2>&1", 2)
  end
end
