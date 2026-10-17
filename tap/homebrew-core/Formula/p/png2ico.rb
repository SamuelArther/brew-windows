class Png2ico < Formula
  desc "PNG to icon converter"
  homepage "https://www.freshports.org/graphics/png2ico/"
  url "https://pkg.freebsd.org/ports-distfiles/png2ico-src-2002-12-08.tar.gz"
  sha256 "d6bc2b8f9dacfb8010e5f5654aaba56476df18d88e344ea1a32523bb5843b68e"
  license "GPL-2.0-only"
  revision 1

  livecheck do
    url :homepage
    regex(/href=.*?png2ico-src[._-]v?(\d+(?:[.-]\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "35efbb469d9c874d232a30e02cdffa1b38ef53093275388f84dd96c2c548d442"
  end

  depends_on "libpng"

  # Fix build with recent clang
  patch :DATA

  def install
    inreplace "Makefile", "g++", "$(CXX)"
    system "make", "CPPFLAGS=#{ENV.cxxflags} #{ENV.cppflags} #{ENV.ldflags}"
    bin.install "png2ico"
    man1.install "doc/png2ico.1"
  end

  test do
    system bin/"png2ico", "out.ico", test_fixtures("test.png")
    assert_path_exists testpath/"out.ico"
  end
end

__END__
diff --git a/png2ico.cpp b/png2ico.cpp
index 8fb87e4..9dedb97 100644
--- a/png2ico.cpp
+++ b/png2ico.cpp
@@ -34,6 +34,8 @@ Notes about transparent and inverted pixels:
 #include <cstdio>
 #include <vector>
 #include <climits>
+#include <cstdlib>
+#include <cstring>

 #if __GNUC__ > 2
 #include <ext/hash_map>
