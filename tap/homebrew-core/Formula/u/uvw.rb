class Uvw < Formula
  desc "Header-only, event based, tiny and easy to use libuv wrapper in modern C++"
  homepage "https://github.com/skypjack/uvw"
  url "https://github.com/skypjack/uvw/archive/refs/tags/v3.4.0_libuv_v1.48.tar.gz"
  version "3.4.0"
  sha256 "c16600573871a5feeb524234b378ab832c8971b2a68d030c6bd0e3077d416ade"
  license "MIT"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)(?:[._-]libuv[._-]v?\d+(?:\.\d+)*)?$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e88973d4aad6aba75b2159e7f0871d66a482487ebe0440774ad8df491489862d"
  end

  depends_on "cmake" => [:build, :test]
  depends_on "pkgconf" => :test
  depends_on "libuv"

  def install
    args = %w[
      -DBUILD_UVW_LIBS=ON
      -DBUILD_DOCS=OFF
    ]
    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"CMakeLists.txt").write <<~CMAKE
      cmake_minimum_required(VERSION 4.0)
      project(test_uvw)

      set(CMAKE_CXX_STANDARD 17)

      find_package(uvw REQUIRED)
      find_package(PkgConfig REQUIRED)
      pkg_check_modules(LIBUV REQUIRED libuv)

      add_executable(test main.cpp)
      target_include_directories(test PRIVATE ${uvw_INCLUDE_DIRS})
      target_link_libraries(test PRIVATE uvw::uvw uv)
    CMAKE

    (testpath/"main.cpp").write <<~CPP
      #include <iostream>
      #include <uvw.hpp>

      int main() {
        auto loop = uvw::loop::get_default();
        auto timer = loop->resource<uvw::timer_handle>();

        timer->on<uvw::timer_event>([](const uvw::timer_event &, uvw::timer_handle &handle) {
          std::cout << "Timer event triggered!" << std::endl;
          handle.close();
        });

        timer->start(uvw::timer_handle::time{1000}, uvw::timer_handle::time{0});
        loop->run();
        return 0;
      }
    CPP

    system "cmake", "-S", ".", "-B", "build"
    system "cmake", "--build", "build"
    system "./build/test"
  end
end
