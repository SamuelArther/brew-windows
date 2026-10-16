class Pngpaste < Formula
  desc "Paste clipboard images into PNG files"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

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
