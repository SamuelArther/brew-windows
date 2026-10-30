class Cli11 < Formula
  desc "Simple and intuitive command-line parser for C++11"
  homepage "https://cliutils.github.io/CLI11/book/"
  url "https://github.com/CLIUtils/CLI11/archive/refs/tags/v2.7.2.tar.gz"
  sha256 "46eef3101da70852ec7af026e09d485ccee81813331c8c6052d39344443b83da"
  license "BSD-3-Clause"
  compatibility_version 1
  head "https://github.com/CLIUtils/CLI11.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "64113fae8b32ba079f685867d2443bee4f845ff1b9616e06b95abfaa6d720eeb"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    args = %w[
      -DCLI11_BUILD_DOCS=OFF
      -DCLI11_BUILD_TESTS=OFF
    ]

    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include "CLI/App.hpp"
      #include "CLI/Formatter.hpp"
      #include "CLI/Config.hpp"

      int main(int argc, char** argv) {
          CLI::App app{"App description"};

          std::string filename = "default";
          app.add_option("-r,--result", filename, "A test string");

          CLI11_PARSE(app, argc, argv);
          std::cout << filename << std::endl;
          return 0;
      }
    CPP
    system ENV.cxx, "-std=c++11", "test.cpp", "-o", "test", "-I#{include}"
    assert_equal "foo\n", shell_output("./test -r foo")
  end
end
