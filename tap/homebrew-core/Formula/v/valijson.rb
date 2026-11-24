class Valijson < Formula
  desc "Header-only C++ library for JSON Schema validation"
  homepage "https://github.com/tristanpenman/valijson"
  url "https://github.com/tristanpenman/valijson/archive/refs/tags/v1.1.5.tar.gz"
  sha256 "49019c677d7290d8ba9c3f3978a76a96f5325dbd4a637de8a5a825a8c92c7ecb"
  license "BSD-2-Clause"
  compatibility_version 1

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "2d103bfdc51d0cc3a664275545428f6fd671292eac7858270816fceaa6b4f4d9"
  end

  depends_on "cmake" => :build

  depends_on "jsoncpp" => :test

  deny_network_access!
  def install
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <valijson/schema.hpp>
      #include <valijson/adapters/jsoncpp_adapter.hpp>
      #include <valijson/utils/jsoncpp_utils.hpp>

      int main (void) { std::cout << "Hello world"; }
    CPP
    system ENV.cxx, "test.cpp", "-std=c++17", "-L#{formula_opt_lib("jsoncpp")}", "-ljsoncpp", "-o", "test"
    system "./test"
  end
end
