class Zmij < Formula
  desc "Fast floating-point-to-string conversion library for C and C++"
  homepage "https://github.com/vitaut/zmij"
  url "https://github.com/vitaut/zmij/archive/refs/tags/v1.2.tar.gz"
  sha256 "13e11c9309fab358d5b6f656050e4f3efa1d20ecf15958ae9055382d07b0268f"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "734d29a8c2d14012da8b6b00302e8013063bbf7c96cec3bb94f84b84f094a13e"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    args = %w[
      -DZMIJ_TEST=OFF
      -DZMIJ_EXAMPLE=OFF
      -DBUILD_SHARED_LIBS=ON
    ]
    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cc").write <<~CPP
      #include <zmij.h>
      #include <cstring>
      #include <iostream>

      int main() {
        char buf[zmij::double_buffer_size + 1];
        auto end = zmij::write(buf, sizeof(buf), 1.23456789e-27);
        *end = 0;
        std::cout << buf;
        return std::strcmp(buf, "1.23456789e-27") == 0 ? 0 : 1;
      }
    CPP

    system ENV.cxx, "test.cc", "-I#{include}", "-L#{lib}", "-lzmij", "-o", "test"
    assert_equal "1.23456789e-27", shell_output("./test")
  end
end
