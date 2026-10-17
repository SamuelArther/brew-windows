class Libresample < Formula
  desc "Audio resampling C library"
  homepage "https://ccrma.stanford.edu/~jos/resample/Available_Software.html"
  url "https://deb.debian.org/debian/pool/main/libr/libresample/libresample_0.1.3.orig.tar.gz"
  sha256 "20222a84e3b4246c36b8a0b74834bb5674026ffdb8b9093a76aaf01560ad4815"
  license "LGPL-2.1-or-later"

  livecheck do
    url "https://deb.debian.org/debian/pool/main/libr/libresample/"
    regex(/href=.*?libresample[._-]v?(\d+(?:\.\d+)+)\.orig\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "cdb660587e6c236e293e12fe460dcea35c1087edf9bbbaf915e2642a7132be1e"
  end

  deny_network_access!

  def install
    system "./configure", *std_configure_args
    system "make"
    lib.install "libresample.a"
    include.install "include/libresample.h"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <libresample.h>

      int main(void) {
        void *handle = resample_open(1, 2.0, 2.0);
        if (!handle) return 1;
        resample_close(handle);
        return 0;
      }
    C
    system ENV.cc, "test.c", "-I#{include}", "-L#{lib}", "-lresample", "-lm", "-o", "test"
    system "./test"
  end
end
