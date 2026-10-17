class SpanLite < Formula
  desc "C++20-like span for C++98, C++11 and later in a single-file header-only library"
  homepage "https://github.com/nonstd-lite/span-lite"
  url "https://github.com/nonstd-lite/span-lite/archive/refs/tags/v0.11.0.tar.gz"
  sha256 "ef4e028e18ff21044da4b4641ca1bc8a2e2d656e2028322876c0e1b9b6904f9d"
  license "BSL-1.0"
  head "https://github.com/nonstd-lite/span-lite.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "a9a3a45dc9f813fb841f4c5361a313a6edbae64e67d63cebf4d243fc21d673a4"
  end

  depends_on "cmake" => :build

  deny_network_access!
  def install
    system "cmake", "-S", ".", "-B", "build", "-DSPAN_LITE_OPT_BUILD_TESTS=OFF", *std_cmake_args
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"basic.cpp").write <<~CPP
      #include "nonstd/span.hpp"
      #include <array>
      #include <vector>
      #include <iostream>

      std::ptrdiff_t size( nonstd::span<const int> spn )
      {
          return spn.size();
      }

      int main()
      {
          int arr[] = { 1, };

          std::cout <<
              "C-array:" << size( arr ) <<
              " array:"  << size( std::array <int, 2>{ 1, 2, } ) <<
              " vector:" << size( std::vector<int   >{ 1, 2, 3, } );
      }
    CPP

    system ENV.cxx, "-std=c++11", "-I#{include}", "basic.cpp", "-o", "basic"
    system "./basic"
  end
end
