class Faceprints < Formula
  desc "Detect and label faces in images using Windows' face detector"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "faceprints", "faceprints"
  end

  test do
    assert_match(/faceprints \d+\.\d+/, shell_output("#{bin}/faceprints --version"))
    assert_match "\"labels\"", shell_output("#{bin}/faceprints list")
    output = shell_output("#{bin}/faceprints extract #{test_fixtures("test.png")} 2>&1", 1)
    assert_match(/no faces detected|not available/, output)
    assert_match "No such file", shell_output("#{bin}/faceprints add someone #{testpath}/missing.jpg 2>&1", 1)
  end
end
