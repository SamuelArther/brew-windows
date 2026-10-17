class Recoverjpeg < Formula
  desc "Tool to recover JPEG images from a file system image"
  homepage "https://rfc1149.net/devel/recoverjpeg.html"
  url "https://rfc1149.net/download/recoverjpeg/recoverjpeg-2.6.3.tar.gz"
  sha256 "db996231e3680bfaf8ed77b60e4027c665ec4b271648c71b00b76d8a627f3201"
  license "GPL-2.0-only"

  livecheck do
    url "https://rfc1149.net/download/recoverjpeg/"
    regex(/href=.*?recoverjpeg[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "d343c838843d5a94a1dc2c51ddea760facfe8f812009f342ed6c5fcf7e404b62"
  end

  def install
    system "./configure", "--disable-dependency-tracking",
                          "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/recoverjpeg -V")
  end
end
