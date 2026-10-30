class Ancient < Formula
  desc "Decompression routines for ancient formats"
  homepage "https://github.com/temisu/ancient"
  url "https://github.com/temisu/ancient/archive/refs/tags/v2.3.0.tar.gz"
  sha256 "5d1d71f0fb8c69955bb4ec01ed9ffd2b5bf546b10463030dda85d949ea422bc9"
  license "BSD-2-Clause"
  compatibility_version 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "4c45fc36c612ec405c9957f0bca251e48d18ab94cb64116bf75faa1a66f4151a"
  end

  depends_on "autoconf" => :build
  depends_on "autoconf-archive" => :build
  depends_on "automake" => :build
  depends_on "libtool" => :build
  depends_on "pkgconf" => :build

  deny_network_access!

  def install
    system "./autogen.sh"
    system "./configure", "--disable-silent-rules", *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <ancient/ancient.hpp>

      int main(int argc, char **argv)
      {
        std::optional<ancient::Decompressor> decompressor;
        return 0;
      }
    CPP

    system ENV.cxx, "-std=c++17", "test.cpp", "-I#{include}", "-L#{lib}", "-lancient", "-o", "test"
    system "./test"
  end
end
