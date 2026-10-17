class Dispenso < Formula
  desc "High-performance C++ library for parallel programming"
  homepage "https://github.com/facebookincubator/dispenso"
  url "https://github.com/facebookincubator/dispenso/archive/refs/tags/v1.6.2.tar.gz"
  sha256 "9641511a6b14a77f68817f1a6e72fa9e55bc3ef2628a10011dcab07c6ba10539"
  license "MIT"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/dispenso-1.6.2"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "3793ec7672446c63d0694082396c20c21724a18fc144f2f2968a7eb0a8495d9c"
  end

  depends_on "cmake" => [:build, :test]

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build",
                    "-DDISPENSO_BUILD_TESTS=OFF",
                    "-DDISPENSO_BUILD_BENCHMARKS=OFF",
                    *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <dispenso/parallel_for.h>
      #include <atomic>
      #include <cstdlib>

      int main() {
          std::atomic<int> sum(0);
          dispenso::parallel_for(0, 100, [&sum](int i) {
              sum += i + 1;
          });
          return sum.load() == 5050 ? EXIT_SUCCESS : EXIT_FAILURE;
      }
    CPP

    (testpath/"CMakeLists.txt").write <<~CMAKE
      cmake_minimum_required(VERSION 3.16)
      project(test_dispenso CXX)
      find_package(Dispenso REQUIRED)
      add_executable(test_dispenso test.cpp)
      target_link_libraries(test_dispenso Dispenso::dispenso)
    CMAKE

    system "cmake", "-S", ".", "-B", "build", "-DCMAKE_CXX_STANDARD=14", *std_cmake_args
    system "cmake", "--build", "build"
    system "./build/test_dispenso"
  end
end
