class Rem < Formula
  desc "Command-line reminders kept and delivered by Windows Task Scheduler"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "rem", "rem"
  end

  test do
    system bin/"rem", "version"
    assert_match "rem add", shell_output("#{bin}/rem help")
    assert_match "Reminders", shell_output("#{bin}/rem ls")
    assert_match "Unknown Reminder List", shell_output("#{bin}/rem ls NoSuchList 2>&1", 255)
  end
end
