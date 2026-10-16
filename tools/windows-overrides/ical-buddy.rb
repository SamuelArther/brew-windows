class IcalBuddy < Formula
  desc "Get events and tasks from the Windows calendar database"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "icalBuddy", "icalBuddy"
  end

  def caveats
    <<~EOS
      icalBuddy reads the calendars and task lists of the accounts added to Windows
      (Settings > Accounts > Email & accounts). If access is refused, allow it in
      Settings > Privacy & security > Calendar (and Tasks).
    EOS
  end

  test do
    assert_match "Non-lossy ASCII", shell_output("#{bin}/icalBuddy strEncodings")
    assert_match version.to_s, shell_output("#{bin}/icalBuddy -V")
    assert_match "unknown command", shell_output("#{bin}/icalBuddy notACommand 2>&1", 2)
    assert_match "cannot parse date", shell_output("#{bin}/icalBuddy eventsFrom:2026-13-45 to:today 2>&1", 2)
  end
end
