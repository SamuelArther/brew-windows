class Quotatool < Formula
  desc "Edit NTFS disk quotas from the command-line"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "quotatool", "quotatool"
  end

  test do
    assert_match "quotatool version", shell_output("#{bin}/quotatool -V")
    assert_match "no group quotas", shell_output("#{bin}/quotatool -g users -q 1 C: 2>&1", 1)
  end
end
