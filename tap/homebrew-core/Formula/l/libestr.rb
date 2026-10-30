class Libestr < Formula
  desc "C library for string handling (and a bit more)"
  homepage "https://libestr.adiscon.com/"
  url "https://libestr.adiscon.com/files/download/libestr-0.1.11.tar.gz"
  sha256 "46632b2785ff4a231dcf241eeb0dcb5fc0c7d4da8ee49cf5687722cdbe8b2024"
  license "LGPL-2.1-or-later"
  compatibility_version 1

  livecheck do
    url "https://libestr.adiscon.com/download/"
    regex(/href=.*?libestr[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "a7009bc2b12d4934fb378f67d41d96a8aff2ead86b7f62dc0719f34c0fab0658"
  end

  depends_on "pkgconf" => :build

  # Fix -flat_namespace being used on Big Sur and later.
  patch do
    file "Patches/libtool/configure-big_sur.diff"
  end

  def install
    system "./configure", *std_configure_args
    system "make"
    system "make", "install"
  end

  test do
    (testpath/"test.c").write <<~C
      #include "stdio.h"
      #include <libestr.h>
      int main() {
        printf("%s\\n", es_version());
        return 0;
      }
    C
    system ENV.cc, "test.c", "-L#{lib}", "-lestr", "-o", "test"
    system "./test"
  end
end
