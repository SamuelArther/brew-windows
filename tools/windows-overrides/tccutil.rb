class Tccutil < Formula
  desc "Utility to modify the Windows privacy permissions (camera, microphone, ...)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "tccutil", "tccutil"
  end

  test do
    assert_match "Unrecognized command \"check\"", shell_output("#{bin}/tccutil check 2>&1")
    assert_match "tccutil 1.5.5", shell_output("#{bin}/tccutil --version")
    system bin/"tccutil", "--list", "--service", "kTCCServiceMicrophone"
    assert_match "no privacy consent", shell_output("#{bin}/tccutil --list 2>&1", 1)
  end
end
