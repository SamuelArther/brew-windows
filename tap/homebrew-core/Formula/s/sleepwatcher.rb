class Sleepwatcher < Formula
  desc "Monitors sleep, wakeup, and idleness of the PC"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "sleepwatcher", "sleepwatcher"
  end

  test do
    assert_match(/sleepwatcher \d+\.\d+/, shell_output("#{bin}/sleepwatcher -v", 2))
    assert_match(/\A\d+\z/, shell_output("#{bin}/sleepwatcher -g").strip)
  end
end
