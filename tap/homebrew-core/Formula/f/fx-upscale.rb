class FxUpscale < Formula
  desc "GPU video upscaling through Media Foundation"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "fx-upscale", "fx-upscale"
  end

  test do
    cp test_fixtures("test.mp4"), testpath
    assert_match "--width", shell_output("#{bin}/fx-upscale --help")
    # Explicit sizes are validated before Media Foundation opens the file (it is optional on Windows Server).
    output = shell_output("#{bin}/fx-upscale --width 20000 #{testpath}/test.mp4 2>&1", 2)
    assert_match "Maximum supported width/height: 16384", output
    assert_match "ProRes", shell_output("#{bin}/fx-upscale --codec prores #{testpath}/test.mp4 2>&1", 1)
    assert_match "No such file", shell_output("#{bin}/fx-upscale #{testpath}/missing.mp4 2>&1", 1)
  end
end
