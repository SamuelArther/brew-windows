class Pngpaste < Formula
  desc "Paste clipboard images into PNG files"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
