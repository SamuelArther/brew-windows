class Gtl < Formula
  desc "Greg's Template Library of useful classes"
  homepage "https://github.com/greg7mdp/gtl"
  url "https://github.com/greg7mdp/gtl/archive/refs/tags/v1.2.0.tar.gz"
  sha256 "1547ab78f62725c380f50972f7a49ffd3671ded17a3cb34305da5c953c6ba8e7"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c94d3c6dde0ea97e16c4a7f781b522b5728879324342690903adb2790f5707b9"
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
