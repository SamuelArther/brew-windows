class Gtl < Formula
  desc "Greg's Template Library of useful classes"
  homepage "https://github.com/greg7mdp/gtl"
  url "https://github.com/greg7mdp/gtl/archive/refs/tags/v1.3.1.tar.gz"
  sha256 "7252bddc62a9075c9f5935c8348715244a8c7573ba95eccf1ce396f67deb11b2"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "1b6e66c77b9043b3956a067a109b71e0f4a4fd26849ad4f362a49ab5c633d938"
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
