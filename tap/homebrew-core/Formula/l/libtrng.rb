class Libtrng < Formula
  desc "Tina's Random Number Generator Library"
  homepage "https://www.numbercrunch.de/trng/"
  url "https://github.com/rabauke/trng4/archive/refs/tags/v4.28.tar.gz"
  sha256 "50432094f8a4b079bcf1c4a9075691c34f4ed5be54835d7ea1a38ccb00ebc89e"
  license "BSD-3-Clause"
  head "https://github.com/rabauke/trng4.git", branch: "master"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/libtrng-4.28"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "9c812fc863f8390fd98c7088a7cc2901342b493016135c9726b5d9d9e8fde08e"
  end

  depends_on "cmake" => :build

  def install
    args = %w[
      -DTRNG_ENABLE_TESTS=OFF
      -DTRNG_ENABLE_EXAMPLES=OFF
    ]
    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <trng/yarn2.hpp>
      #include <trng/normal_dist.hpp>
      int main()
      {
        trng::yarn2 R;
        trng::normal_dist<> normal(6.0, 2.0);
        (void)normal(R);
        return 0;
      }
    CPP
    system ENV.cxx, "-std=c++11", "test.cpp", "-o", "test", "-I#{include}", "-L#{lib}", "-ltrng4"
    system "./test"
  end
end
