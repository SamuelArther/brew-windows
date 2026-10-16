class Tccutil < Formula
  desc "Utility to modify the Windows privacy permissions (camera, microphone, ...)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
