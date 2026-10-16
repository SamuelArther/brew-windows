class Displayplacer < Formula
  desc "Utility to configure multi-display resolutions and arrangements"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "displayplacer", "displayplacer"
  end

  test do
    assert_match "Execute the command below", shell_output("#{bin}/displayplacer list")
    assert_match(/displayplacer v\d+\.\d+/, shell_output("#{bin}/displayplacer --version"))
    assert_match "bad res", shell_output("#{bin}/displayplacer \"id:1 res:axb\" 2>&1", 2)
  end
end
