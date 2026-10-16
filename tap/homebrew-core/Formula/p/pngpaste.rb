class Pngpaste < Formula
  desc "Paste clipboard images into PNG files"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

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
