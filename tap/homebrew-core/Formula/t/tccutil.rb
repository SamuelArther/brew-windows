class Tccutil < Formula
  desc "Utility to modify the Windows privacy permissions (camera, microphone, ...)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "dc127cb6087c083d80b876f8a47e29ca06fb328465f7d50d61cd35c0b51e0977"
  end

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
