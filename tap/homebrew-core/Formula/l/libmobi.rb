class Libmobi < Formula
  desc "C library for handling Kindle (MOBI) formats of ebook documents"
  homepage "https://github.com/bfabiszewski/libmobi"
  url "https://github.com/bfabiszewski/libmobi/releases/download/v0.12/libmobi-0.12.tar.gz"
  sha256 "9a6fb2c56b916f8fa8b15e0c71008d908109508c944ea1d297881d4e277bf7e7"
  license "LGPL-3.0-or-later"
  revision 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "220b8d9e6155008eccfb96c93ec8164e3e9c5bae9b48298379964aa6f2312019"
  end

  uses_from_macos "libxml2"

  on_linux do
    depends_on "zlib-ng-compat"
  end

  # Fix -flat_namespace being used on Big Sur and later.
  patch do
    file "Patches/libtool/configure-big_sur.diff"
  end

  deny_network_access!

  def install
    system "./configure", *std_configure_args, "--disable-silent-rules"
    system "make", "install"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <mobi.h>
      int main() {
        MOBIData *m = mobi_init();
        if (m == NULL) {
          return 1;
        }
        mobi_free(m);
        return 0;
      }
    CPP
    system ENV.cxx, "test.cpp", "-I#{include}", "-L#{lib}", "-lmobi", "-o", "test"
    system "./test"
  end
end
