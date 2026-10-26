class ApacheArrowAdbc < Formula
  desc "Cross-language, Arrow-native database access"
  homepage "https://arrow.apache.org/adbc"
  url "https://www.apache.org/dyn/closer.lua?path=arrow/apache-arrow-adbc-25/apache-arrow-adbc-25.tar.gz"
  sha256 "de2910f951eeb3c57d882e621a32d133580cc6b0ad01ac25056f4ac709b8b2c0"
  license "Apache-2.0"
  compatibility_version 1
  head "https://github.com/apache/arrow-adbc.git", branch: "main"

  livecheck do
    url "https://archive.apache.org/dist/arrow/"
    regex(%r{href=["']?apache-arrow-adbc[._-]v?(\d+(?:\.\d+)*)/}i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "646789e32a0ee39bd1fb50d81c3f17bd8caafc5f1786baf84bdf9941718a465d"
  end

  depends_on "cmake" => :build
  depends_on "pkgconf" => :build

  deny_network_access!

  def install
    args = %w[
      -DADBC_BUILD_STATIC=OFF
      -DADBC_BUILD_SHARED=ON
      -DADBC_DRIVER_MANAGER=ON
      -DADBC_DRIVER_POSTGRESQL=OFF
      -DADBC_DRIVER_SQLITE=OFF
    ]
    system "cmake", "-S", "c", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include "arrow-adbc/adbc.h"
      int main(void) {
        struct AdbcError error;
        return 0;
      }
    CPP
    system ENV.cxx, "test.cpp", "-std=c++17", "-I#{include}", "-L#{lib}", "-ladbc_driver_manager", "-o", "test"
    system "./test"
  end
end
