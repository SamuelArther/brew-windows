class Libicns < Formula
  desc "Library for manipulation of the macOS .icns resource format"
  homepage "https://icns.sourceforge.io/"
  url "https://downloads.sourceforge.net/project/icns/libicns-0.8.1.tar.gz"
  mirror "https://deb.debian.org/debian/pool/main/libi/libicns/libicns_0.8.1.orig.tar.gz"
  sha256 "335f10782fc79855cf02beac4926c4bf9f800a742445afbbf7729dab384555c2"
  license any_of: ["LGPL-2.0-or-later", "LGPL-2.1-or-later"]
  revision 5

  bottle do
    sha256 cellar: :any, x86_64_windows: "d80205a46f573e77d6a66a17553567078be1093b77ce30a80a715aaa63b3fa25"
  end

  depends_on "jasper"
  depends_on "libpng"

  # Fix -flat_namespace being used on Big Sur and later.
  patch do
    file "Patches/libtool/configure-pre-0.4.2.418-big_sur.diff"
  end

  def install
    # Fix for libpng 1.5
    inreplace "icnsutils/png2icns.c",
      "png_set_gray_1_2_4_to_8",
      "png_set_expand_gray_1_2_4_to_8"

    # Fix compile with newer Clang
    ENV.append_to_cflags "-Wno-implicit-function-declaration" if DevelopmentTools.clang_build_version >= 1403

    system "./configure", "--disable-debug", "--disable-dependency-tracking",
                          "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <stdio.h>
      #include "icns.h"
      int main(void)
      {
        int    error = 0;
        FILE            *inFile = NULL;
        icns_family_t  *iconFamily = NULL;
        icns_image_t  iconImage;
        return 0;
      }
    C
    system ENV.cc, "-L#{lib}", "-licns", testpath/"test.c", "-o", "test"
    system "./test"
  end
end
