class Streamvbyte < Formula
  desc "Fast integer compression in C"
  homepage "https://fast-pack.github.io/streamvbyte/"
  url "https://github.com/fast-pack/streamvbyte/archive/refs/tags/v3.2.1.tar.gz"
  sha256 "838195fd3347c52735f9c44922fb6e2a509fd31db40fdf892b42347548a1fe3a"
  license "Apache-2.0"
  head "https://github.com/fast-pack/streamvbyte.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "3212ddd2d137b5fe81b58a09e8420bc3206b7e32e207b30f0bc4a8c0c2cd5c83"
  end

  depends_on "cmake" => :build

  def install
    system "cmake", "-S", ".", "-B", "build", "-DCMAKE_POLICY_VERSION_MINIMUM=3.5", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"

    pkgshare.install "examples/example.c"
  end

  test do
    system ENV.cc, pkgshare/"example.c", "-I#{include}", "-L#{lib}", "-lstreamvbyte", "-o", "test"
    system testpath/"test"
  end
end
