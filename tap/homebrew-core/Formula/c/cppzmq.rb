class Cppzmq < Formula
  desc "Header-only C++ binding for libzmq"
  homepage "https://www.zeromq.org"
  url "https://github.com/zeromq/cppzmq/archive/refs/tags/v4.11.0.tar.gz"
  sha256 "0fff4ff311a7c88fdb76fceefba0e180232d56984f577db371d505e4d4c91afd"
  license "MIT"
  compatibility_version 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "7c86b7b2856413dcf6fc393150e4017ea27be172893885f53716f38ea64066de"
  end

  depends_on "cmake" => :build
  depends_on "pkgconf" => :build
  depends_on "zeromq"

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", "-DCPPZMQ_BUILD_TESTS=OFF", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"

    pkgshare.install "examples"
  end

  test do
    cp pkgshare/"examples/hello_world.cpp", testpath
    system ENV.cxx, "-std=c++11", "hello_world.cpp", "-I#{include}",
                    "-L#{formula_opt_lib("zeromq")}", "-lzmq", "-o", "test"
    system "./test"
  end
end
