class Reproc < Formula
  desc "Cross-platform (C99/C++11) process library"
  homepage "https://github.com/daandemeyer/reproc"
  url "https://github.com/daandemeyer/reproc/archive/refs/tags/14.2.8.tar.gz"
  sha256 "27c3b452bfc419a2deda23969aa10c77909c4ff9e71c549eb65d09ae6aa7aa32"
  license "MIT"
  compatibility_version 1
  head "https://github.com/DaanDeMeyer/reproc.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "0d73583db667d25dc36089b479a2196038f1c6893c2c90fd8b8e7863ee3df802"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    args = *std_cmake_args << "-DREPROC++=ON"
    system "cmake", "-S", ".", "-B", "build", *args, "-DBUILD_SHARED_LIBS=ON", "-DCMAKE_INSTALL_RPATH=#{rpath}"
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
    rm_r("build")
    system "cmake", "-S", ".", "-B", "build", *args
    system "cmake", "--build", "build"
    lib.install "build/reproc/lib/libreproc.a", "build/reproc++/lib/libreproc++.a"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <reproc/run.h>

      int main(void) {
        const char *args[] = { "echo", "Hello, world!", NULL };
        return reproc_run(args, (reproc_options) { 0 });
      }
    C

    (testpath/"test.cpp").write <<~CPP
      #include <iostream>
      #include <reproc++/run.hpp>

      int main(void) {
        int status = -1;
        std::error_code ec;

        const char *args[] = { "echo", "Hello, world!", NULL };
        reproc::options options;

        std::tie(status, ec) = reproc::run(args, options);
        return ec ? ec.value() : status;
      }
    CPP

    system ENV.cc, "test.c", "-I#{include}", "-L#{lib}", "-lreproc", "-o", "test-c"
    system ENV.cxx, "test.cpp", "-std=c++11", "-I#{include}", "-L#{lib}", "-lreproc++", "-o", "test-cpp"

    assert_equal "Hello, world!", shell_output("./test-c").chomp
    assert_equal "Hello, world!", shell_output("./test-cpp").chomp
  end
end
