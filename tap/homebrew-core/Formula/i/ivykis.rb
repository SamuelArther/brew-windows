class Ivykis < Formula
  desc "Async I/O-assisting library"
  homepage "https://sourceforge.net/projects/libivykis/"
  url "https://github.com/buytenh/ivykis/archive/refs/tags/v0.43.2-trunk.tar.gz"
  sha256 "22621ae6a7144039cfb8666ed509b99ea1876d7642021a3505c7351502641103"
  license "LGPL-2.1-only"
  compatibility_version 1

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)(?:[._-]trunk)?$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "3bc26a3b73f26935de1cf1aa6728a96b6b7e1a1ae16e2c65bf00f8c54452cd0a"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build
  depends_on "libtool" => :build

  deny_network_access!

  def install
    system "autoreconf", "--force", "--install", "--verbose"
    system "./configure", *std_configure_args
    system "make"
    system "make", "check"
    system "make", "install"
  end

  test do
    (testpath/"test_ivykis.c").write <<~C
      #include <stdio.h>
      #include <iv.h>
      int main()
      {
        iv_init();
        iv_deinit();
        return 0;
      }
    C
    system ENV.cc, "test_ivykis.c", "-L#{lib}", "-livykis", "-o", "test_ivykis"
    system "./test_ivykis"
  end
end
