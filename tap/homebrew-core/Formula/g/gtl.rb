class Gtl < Formula
  desc "Greg's Template Library of useful classes"
  homepage "https://github.com/greg7mdp/gtl"
  url "https://github.com/greg7mdp/gtl/archive/refs/tags/v1.3.0.tar.gz"
  sha256 "e19a0a0d426a6a96d0fd4505c61f1b689ea12e37c1aa1729d4947493cfdc9d2b"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "5b24126a2ff3cb7c5c92b81c4c247dc11e6b4bcbbd83022be73d89e2f51f5656"
  end

  depends_on "boost" => :build
  depends_on "cmake" => :build

  def install
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"

    pkgshare.install "examples/btree"
  end

  test do
    cp_r Dir[pkgshare/"btree/*"], testpath
    system ENV.cxx, "btree.cpp", "-std=c++20", "-I#{include}", "-o", "test"
    system "./test"
  end
end
