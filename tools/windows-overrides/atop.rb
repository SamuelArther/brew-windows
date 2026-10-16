class Atop < Formula
  desc "Advanced system and process monitor using Windows performance counters"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "atop", "atop"
  end

  test do
    assert_match "Version:", shell_output("#{bin}/atop -V")
    system bin/"atop", "1", "1"
    system bin/"atop", "-w", "atop.raw", "1", "1"
    assert_match "SEP", shell_output("#{bin}/atop -r atop.raw -PCPU,DSK")
  end
end
