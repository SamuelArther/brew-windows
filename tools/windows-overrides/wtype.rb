class Wtype < Formula
  desc "Type text and keys via synthesized keyboard input"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
