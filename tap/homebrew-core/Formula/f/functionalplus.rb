class Functionalplus < Formula
  desc "Functional Programming Library for C++"
  homepage "https://www.daiw.de/fplus-api-search/"
  url "https://github.com/Dobiasd/FunctionalPlus/archive/refs/tags/v0.2.28.tar.gz"
  sha256 "8864a3e9bebde6ebed71b49ac2a036cedf9ae0f02ce758bc28c21e6a2ae15803"
  license "BSL-1.0"
  head "https://github.com/Dobiasd/FunctionalPlus.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/functionalplus-0.2.28"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "ef1f336c9c102c3c9d738394cf54f14df071a7413084c6043d9dc4df99bb97e0"
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
      #include <fplus/fplus.hpp>
      #include <iostream>
      int main() {
        std::list<std::string> things = {"same old", "same old"};
        if (fplus::all_the_same(things))
          std::cout << "All things being equal." << std::endl;
      }
    CPP
    system ENV.cxx, "-std=c++14", "test.cpp", "-I#{include}", "-o", "test"
    assert_match "All things being equal.", shell_output("./test")
  end
end
