class Wtype < Formula
  desc "Type text and keys via synthesized keyboard input"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "wtype", "wtype"
  end

  test do
    assert_match "Usage: wtype", shell_output("#{bin}/wtype --help")
    assert_match "Unknown key", shell_output("#{bin}/wtype -k NoSuchKey 2>&1", 2)
    assert_match "Invalid modifier", shell_output("#{bin}/wtype -M hyper x 2>&1", 2)
  end
end
