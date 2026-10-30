class Octomap < Formula
  desc "Efficient probabilistic 3D mapping framework based on octrees"
  homepage "https://octomap.github.io/"
  url "https://github.com/OctoMap/octomap/archive/refs/tags/v1.10.1.tar.gz"
  sha256 "b6b6c10c99ab15701dd105840e7d4cf18e226eb68714dd4bdfe049dede5cd489"
  license "BSD-3-Clause"
  compatibility_version 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "475d66b97f7dc017caee4080dd10bd4fd9e6364dabda988278f74fb8197bb71a"
  end

  depends_on "cmake" => :build
  depends_on "pkgconf" => :test

  deny_network_access!

  def install
    # GCC 16 defaults to C++20, where <ciso646> warns and octomap builds with -Werror.
    args = OS.windows? ? ["-DCMAKE_CXX_STANDARD=17"] : []
    system "cmake", "-S", "octomap", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <cassert>
      #include <octomap/octomap.h>
      int main() {
        octomap::OcTree tree(0.05);
        assert(tree.size() == 0);
        return 0;
      }
    CPP

    flags = shell_output("pkgconf --cflags --libs octomap").chomp.split
    system ENV.cxx, "test.cpp", "-o", "test", *flags
    system "./test"
  end
end
