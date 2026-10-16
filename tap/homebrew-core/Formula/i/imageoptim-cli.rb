class ImageoptimCli < Formula
  desc "Losslessly optimise PNG, JPEG and GIF images"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "imageoptim", "imageoptim"
  end

  test do
    assert_match(/\d+\.\d+/, shell_output("#{bin}/imageoptim -V"))
    cp test_fixtures("test.png"), testpath/"test.png"
    assert_match "test.png", shell_output("#{bin}/imageoptim --dry-run --no-color test.png")
    assert_match "TOTAL", shell_output("#{bin}/imageoptim --no-color test.png")
    assert_equal "\x89PNG".b, (testpath/"test.png").binread[0, 4]
  end
end
