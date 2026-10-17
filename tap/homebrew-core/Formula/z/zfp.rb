class Zfp < Formula
  desc "Compressed numerical arrays that support high-speed random access"
  homepage "https://zfp.llnl.gov"
  url "https://github.com/llnl/zfp/archive/refs/tags/1.0.1.tar.gz"
  sha256 "4984db6a55bc919831966dd17ba5e47ca7ac58668f4fd278ebd98cd2200da66f"
  license "BSD-3-Clause"
  head "https://github.com/llnl/zfp.git", branch: "develop"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "ad35329ffc351b469aabdf8923ff1a31187549d5dd7be484b0be275a7434b9e8"
  end

  depends_on "cmake" => [:build, :test]

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <iostream>
      #include <vector>
      #include <zfp/array2.hpp>

      int main()
      {
        const size_t nx = 12;
        const size_t ny = 8;
        const double bits_per_value = 4.0;

        zfp::array2<double> arr(nx, ny, bits_per_value);

        for (size_t y = 0; y < ny; y++) {
          for (size_t x = 0; x < nx; x++) {
            arr(x, y) = x + nx * y;
          }
        }

        arr.flush_cache();
        std::cout << "zfp bytes = " << arr.size_bytes(ZFP_DATA_PAYLOAD) << std::endl;

        return 0;
      }
    CPP

    (testpath/"CMakeLists.txt").write <<~CMAKE
      cmake_minimum_required(VERSION 4.0)
      project(test_zfp)

      set(CMAKE_CXX_STANDARD 11)

      find_package(zfp REQUIRED)

      add_executable(test test.cpp)
      target_link_libraries(test PUBLIC zfp)
    CMAKE

    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "./build/test"
  end
end
