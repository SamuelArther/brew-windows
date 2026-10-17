class Libsais < Formula
  desc "Fast linear time suffix array, lcp array and bwt construction"
  homepage "https://github.com/IlyaGrebnov/libsais"
  url "https://github.com/IlyaGrebnov/libsais/archive/refs/tags/v2.10.4.tar.gz"
  sha256 "94aa88f9e29f8812214ecfa6b55f5dca14c7d8a427409c062f734a61ca4c6931"
  license "Apache-2.0"
  head "https://github.com/IlyaGrebnov/libsais.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/libsais-2.10.4"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e14e77b02643b2ad0eaa19a3efa19be03154d1695a1bbabe50df34f338cb270b"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", "-DLIBSAIS_BUILD_SHARED_LIB=ON", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <libsais.h>
      #include <stdlib.h>
      int main() {
        uint8_t input[] = "homebrew";
        int32_t sa[8];
        libsais(input, sa, 8, 0, NULL);

        if (sa[0] == 4 &&
            sa[1] == 3 &&
            sa[2] == 6 &&
            sa[3] == 0 &&
            sa[4] == 2 &&
            sa[5] == 1 &&
            sa[6] == 5 &&
            sa[7] == 7) {
            return 0;
        }
        return 1;
      }
    C
    system ENV.cc, "test.c", "-I#{include}", "-L#{lib}", "-lsais", "-o", "test"
    system "./test"
  end
end
