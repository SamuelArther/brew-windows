class Cotila < Formula
  desc "Compile-time linear algebra system for C++"
  homepage "https://github.com/calebzulawski/cotila"
  url "https://github.com/calebzulawski/cotila/archive/refs/tags/1.2.1.tar.gz"
  sha256 "898ebfdf562cd1a3622870e17a703b38559cf2c607b2d5f79e6b3a55563af619"
  license "Apache-2.0"
  head "https://github.com/calebzulawski/cotila.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/cotila-1.2.1"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "d6670b01ea49ec38585e25b054053cf0f700f59790c91c874e3d395cbc0d3fa6"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <iostream>
      #include <cotila/cotila.h>

      int main() {
        constexpr cotila::vector<double, 3> v1 {{1., -2., 3.}};
        constexpr cotila::vector v2 {1., 2., 3.};
        static_assert(v2 == cotila::abs(v1));

        constexpr cotila::matrix<double, 2, 3> m1 {{{1., 2., 3.}, {4., 5., 6.}}};
        constexpr cotila::matrix m2 {{{1., 4.}, {2., 5.}, {3., 6.}}};
        static_assert(m2 == cotila::transpose(m1));

        constexpr cotila::matrix<std::complex<double>, 2, 2> m3 {{{{1., 0.}, {2., 1.}}, {{3., -1.}, {4., 2.}}}};
        constexpr cotila::matrix m4 {{{{1., 0.}, {3., 1.}}, {{2., -1.}, {4., -2.}}}};
        static_assert(m4 == cotila::hermitian(m3));

        constexpr double s = cotila::sqrt(4.);
        static_assert(s == 2.);
        std::cout << s << std::endl;
      }
    CPP

    system ENV.cxx, "test.cpp", "-std=c++17", "-I#{include}", "-o", "test"
    assert_equal "2\n", shell_output("./test")
  end
end
