class MacCleanupGo < Formula
  desc "Cleaner that scans caches, logs and temp files and removes what you choose"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "mac-cleanup", "mac-cleanup"
  end

  test do
    assert_match(/\d+\.\d+/, shell_output("#{bin}/mac-cleanup --version"))
    assert_match "Scan Windows cache", shell_output("#{bin}/mac-cleanup --help")
    assert_match "Dry Run Report", shell_output("#{bin}/mac-cleanup --clean --dry-run")
    assert_match "Temporary Files", shell_output("#{bin}/mac-cleanup --select")
  end
end
