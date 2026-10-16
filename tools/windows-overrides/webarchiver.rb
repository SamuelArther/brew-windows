class Webarchiver < Formula
  desc "Create Safari .webarchive files"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "webarchiver", "webarchiver"
  end

  test do
    (testpath/"page.html").write <<~HTML
      <html><head><link rel="stylesheet" href="style.css"></head>
      <body><img src="logo.png"></body></html>
    HTML
    (testpath/"style.css").write "body { background: url('logo.png'); }"
    (testpath/"logo.png").write "not really a png"
    system bin/"webarchiver", "-url", "file:///#{testpath}/page.html", "-output", "foo.webarchive"
    archive = (testpath/"foo.webarchive").binread
    assert_equal "bplist00", archive[0, 8]
    assert_match "WebSubresources", archive
    assert_match "style.css", archive
    assert_match "Usage: webarchiver", shell_output("#{bin}/webarchiver -help")
  end
end
