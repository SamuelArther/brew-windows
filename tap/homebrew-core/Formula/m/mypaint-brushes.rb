class MypaintBrushes < Formula
  desc "Brushes used by MyPaint and other software using libmypaint"
  homepage "https://github.com/mypaint/mypaint-brushes"
  url "https://github.com/mypaint/mypaint-brushes/archive/refs/tags/v2.0.2.tar.gz"
  sha256 "01032550dd817bb0f8e85d83a632ed2e50bc16e0735630839e6c508f02f800ac"
  license "CC0-1.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "e2cdc4108d86ee72c407fb5d666c25be915542c66c9e9f2abc20d3f5b7b3e635"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build

  depends_on "libmypaint"

  def install
    ENV["ACLOCAL"] = "aclocal"
    ENV["AUTOMAKE"] = "automake"

    system "./autogen.sh"
    system "./configure", *std_configure_args, "--disable-silent-rules"
    system "make"
    system "make", "install"
  end

  test do
    assert_path_exists share.glob("mypaint-data/*/brushes/classic/marker_small_prev.png").first
  end
end
