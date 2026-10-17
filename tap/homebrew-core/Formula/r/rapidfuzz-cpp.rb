class RapidfuzzCpp < Formula
  desc "Rapid fuzzy string matching in C++ using the Levenshtein Distance"
  homepage "https://rapidfuzz.github.io/rapidfuzz-cpp/"
  url "https://github.com/rapidfuzz/rapidfuzz-cpp/archive/refs/tags/v3.3.4.tar.gz"
  sha256 "a0dd2ef361cac165e12076696e7c7e8d069a2908abd9599ad4bd190de33f9881"
  license "MIT"
  head "https://github.com/rapidfuzz/rapidfuzz-cpp.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "33591211a5a39addc10311ed4f1fed51a47f91644dd0ac8236e825abded34da1"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <rapidfuzz/fuzz.hpp>
      #include <string>
      #include <iostream>

      int main()
      {
          std::string a = "aaaa";
          std::string b = "abab";
          std::cout << rapidfuzz::fuzz::ratio(a, b) << std::endl;
      }
    CPP
    system ENV.cxx, "test.cpp", "-std=c++17", "-I#{include}", "-o", "test"
    assert_equal "50", shell_output("./test").strip
  end
end
