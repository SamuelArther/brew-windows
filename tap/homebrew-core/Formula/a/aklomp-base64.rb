class AklompBase64 < Formula
  desc "Fast Base64 stream encoder/decoder in C99, with SIMD acceleration"
  homepage "https://github.com/aklomp/base64"
  url "https://github.com/aklomp/base64/archive/refs/tags/v0.5.2.tar.gz"
  sha256 "723a0f9f4cf44cf79e97bcc315ec8f85e52eb104c8882942c3f2fba95acc080d"
  license "BSD-2-Clause"
  compatibility_version 1

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "d15bdb94ed8d00f3de5e1cf03d7d87b465c9f6b078e503882c4f137960420040"
  end

  depends_on "cmake" => :build

  conflicts_with "base64", because: "both install `base64` binaries"

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build",
                    "-DBUILD_SHARED_LIBS=ON",
                    "-DCMAKE_INSTALL_RPATH=#{rpath}",
                    *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    assert_equal "aGVsbG8=", pipe_output(bin/"base64", "hello", 0).strip
  end
end
