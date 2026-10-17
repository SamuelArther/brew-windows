class Ctre < Formula
  desc "Compile-time regular expression matcher for C++"
  homepage "https://compile-time-regular-expressions.readthedocs.io/"
  url "https://github.com/hanickadot/compile-time-regular-expressions/archive/refs/tags/v3.11.0.tar.gz"
  sha256 "7d4b30d0bdd8864a47cceb2ab8e7c4d1846f0ec62383f8c45122435d32f19530"
  license "Apache-2.0"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/ctre-3.11.0"
    sha256 cellar: :any, x86_64_windows: "77d81b38275b3c46aab2a757665c92f4d49e5c9725601c4450475a2473758e95"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.c++").write <<~'CXX'
      #include <ctre-unicode.hpp>

      int main()
      {
        constexpr auto url = "https://github.com/org/repo/archive/refs/tags/v1.6.18.tar.gz";
        constexpr auto regex = ctll::fixed_string{R"(\d+(?:\.\d+)+)"};
        constexpr auto version = ctre::search<regex>(url);
        static_assert(version && version.get<0>() == "1.6.18");
      }
    CXX
    system ENV.cxx, "test.c++", "-o", "test", "-std=c++20", "-I#{include}"
    system "./test"
  end
end
