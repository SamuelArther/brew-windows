class HopscotchMap < Formula
  desc "C++ implementation of a fast hash map and hash set using hopscotch hashing"
  homepage "https://github.com/Tessil/hopscotch-map"
  url "https://github.com/Tessil/hopscotch-map/archive/refs/tags/v2.4.0.tar.gz"
  sha256 "c147d1f6af9559c0e91af3ecf62274404ce5fb35ce94d2234c080ccc7a5913de"
  license "MIT"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/hopscotch-map-2.4.0"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "97e56b22b7ff941c73d7af13ecd71d2bae53b47c4171c8b95650c531491e2cdf"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cc").write <<~CPP
      #include <tsl/hopscotch_set.h>
      #include <cassert>

      int main() {
        tsl::hopscotch_set<int> s;
        s.insert(3);
        assert(s.count(3) == 1);
        return(0);
      }
    CPP
    system ENV.cxx, "-std=c++17", "test.cc", "-I#{include}", "-o", "test"
    system "./test"
  end
end
