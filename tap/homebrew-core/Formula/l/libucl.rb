class Libucl < Formula
  desc "Universal configuration library parser"
  homepage "https://github.com/vstakhov/libucl"
  url "https://github.com/vstakhov/libucl/archive/refs/tags/0.10.0.tar.gz"
  sha256 "3e27a97d48561aa262f93715ad5b483dceca02b033662b9a5a29c36f48b518e7"
  license "BSD-2-Clause"

  bottle do
    sha256 cellar: :any, x86_64_windows: "4d231d150f3c9fd139d559415517052b337bc38ad1b27fcc596543807aab3b7c"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build
  depends_on "libtool" => :build
  depends_on "pkgconf" => :build

  def install
    system "./autogen.sh"

    args = %w[
      --disable-silent-rules
      --enable-utils
    ]

    system "./configure", *args, *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <ucl++.h>
      #include <string>
      #include <cassert>

      int main(int argc, char **argv) {
        const std::string cfg = "foo = bar; section { flag = true; }";
        std::string err;
        auto obj = ucl::Ucl::parse(cfg, err);
        assert(obj);
        assert(obj[std::string("foo")].string_value() == "bar");
        assert(obj[std::string("section")][std::string("flag")].bool_value());
      }
    CPP
    system ENV.cxx, "-std=c++11", "test.cpp", "-L#{lib}", "-lucl", "-o", "test"
    system "./test"
  end
end
