class Mstch < Formula
  desc "Complete implementation of {{mustache}} templates using modern C++"
  homepage "https://github.com/no1msd/mstch"
  url "https://github.com/no1msd/mstch/archive/refs/tags/1.0.2.tar.gz"
  sha256 "811ed61400d4e9d4f9ae0f7679a2ffd590f0b3c06b16f2798e1f89ab917cba6c"
  license "MIT"
  revision 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "68fd2f080725d059a6045617755176aba46b62adb8427b2cac308d89f4a3dbff"
  end

  depends_on "cmake" => :build
  depends_on "boost"

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", "-DCMAKE_POLICY_VERSION_MINIMUM=3.5", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"

    (lib/"pkgconfig/mstch.pc").write pc_file
  end

  def pc_file
    <<~EOS
      prefix=#{HOMEBREW_PREFIX}
      exec_prefix=${prefix}
      libdir=${exec_prefix}/lib
      includedir=${exec_prefix}/include

      Name: mstch
      Description: Complete implementation of {{mustache}} templates using modern C++
      Version: 1.0.1
      Libs: -L${libdir} -lmstch
      Cflags: -I${includedir}
    EOS
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <mstch/mstch.hpp>
      #include <cassert>
      #include <string>
      int main() {
        std::string view("Hello, world");
        mstch::map context;

        assert(mstch::render(view, context) == "Hello, world");
      }
    CPP

    system ENV.cxx, "test.cpp", "-L#{lib}", "-lmstch", "-std=c++11", "-o", "test"
    system "./test"
  end
end
