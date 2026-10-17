class Ps2eps < Formula
  desc "Convert PostScript to EPS files"
  homepage "https://github.com/roland-bless/ps2eps"
  url "https://github.com/roland-bless/ps2eps/archive/refs/tags/v1.72.tar.gz"
  sha256 "03bd71d8f1cc86369ec865486e8d371f3bd9a3741e86c9f0780797b9ab0a3fd4"
  license "GPL-2.0-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "f03c520e6faf487ff19432495767c72df286a8826213ce52e2e92602ef14acc6"
  end

  depends_on "ghostscript"

  def install
    system ENV.cc, "src/C/bbox.c", "-o", "bbox"
    bin.install "bbox"
    (libexec/"bin").install "src/perl/ps2eps"
    (bin/"ps2eps").write <<~SH
      #!/bin/sh
      perl -S #{libexec}/bin/ps2eps "$@"
    SH
    man1.install Dir["doc/*.1"]
    doc.install Dir["doc/*.pdf", "doc/*.html"]
  end

  test do
    cp test_fixtures("test.ps"), testpath/"test.ps"
    system bin/"ps2eps", testpath/"test.ps"
    assert_path_exists testpath/"test.eps"
  end
end
