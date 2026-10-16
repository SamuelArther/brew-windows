class Pngpaste < Formula
  desc "Paste clipboard images into PNG files"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "pngpaste", "pngpaste"
  end

  test do
    assert_match "pngpaste", shell_output("#{bin}/pngpaste -v")
    # The CI desktop's clipboard holds no image.
    assert_match "No image data found on the clipboard", shell_output("#{bin}/pngpaste test.png 2>&1", 1)
  end
end
