class Tlx < Formula
  desc "Collection of Sophisticated C++ Data Structures, Algorithms and Helpers"
  homepage "https://tlx.github.io"
  url "https://github.com/tlx/tlx/archive/refs/tags/v0.6.1.tar.gz"
  sha256 "24dd1acf36dd43b8e0414420e3f9adc2e6bb0e75047e872a06167961aedad769"
  license "BSL-1.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "28a3f8c97e52af43e8c96b8a9fb52326217d2076fe528a0f3da536918eda973b"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    # Workaround for CMake 4.0+
    args = %w[-DCMAKE_POLICY_VERSION_MINIMUM=3.5]
    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <tlx/math/aggregate.hpp>
      int main()
      {
        tlx::Aggregate<int> agg;
        for (int i = 0; i < 30; ++i) {
          agg.add(i);
        }
        return 0;
      }
    CPP
    system ENV.cxx, "test.cpp", "-L#{lib}", "-ltlx", "-o", "test", "-std=c++17"
    system "./test"
  end
end
