class MacCleanupGo < Formula
  desc "Cleaner that scans caches, logs and temp files and removes what you choose"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "mac-cleanup", "mac-cleanup"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mac-cleanup --version")
    assert_match "Scan Windows cache", shell_output("#{bin}/mac-cleanup --help")
    assert_match "Dry Run Report", shell_output("#{bin}/mac-cleanup --clean --dry-run")
    assert_match "Temporary Files", shell_output("#{bin}/mac-cleanup --select")
  end
end
