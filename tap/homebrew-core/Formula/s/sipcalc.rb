class Sipcalc < Formula
  desc "Advanced console-based IP subnet calculator"
  homepage "https://www.routemeister.net/projects/sipcalc/"
  url "https://www.routemeister.net/projects/sipcalc/files/sipcalc-1.1.6.tar.gz"
  sha256 "cfd476c667f7a119e49eb5fe8adcfb9d2339bc2e0d4d01a1d64b7c229be56357"
  license "BSD-3-Clause"

  livecheck do
    url "https://www.routemeister.net/projects/sipcalc/download.html"
    regex(/href=.*?sipcalc[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "ee64d9e7cc6eca68f0818bae8169fe4874ecb1ff8c8b4bc44dcc95a0ae2945ee"
  end

  deny_network_access!

  def install
    system "./configure", "--disable-debug",
                          "--disable-dependency-tracking",
                          "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    system bin/"sipcalc", "-h"
  end
end
