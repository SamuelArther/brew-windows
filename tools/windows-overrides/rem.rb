class Rem < Formula
  desc "Command-line reminders kept and delivered by Windows Task Scheduler"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
