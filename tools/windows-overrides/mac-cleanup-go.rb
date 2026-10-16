class MacCleanupGo < Formula
  desc "Cleaner that scans caches, logs and temp files and removes what you choose"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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
