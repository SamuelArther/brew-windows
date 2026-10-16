class Zzz < Formula
  desc "Command-line tool to put the PC to sleep"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "Zzz", "Zzz"
  end

  test do
    assert_match "Puts the PC to sleep", shell_output("#{bin}/Zzz -h")
    assert_match "unknown option", shell_output("#{bin}/Zzz --bogus 2>&1", 2)
  end
end
