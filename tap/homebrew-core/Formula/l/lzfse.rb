class Lzfse < Formula
  desc "Apple LZFSE compression library and command-line tool"
  homepage "https://github.com/lzfse/lzfse"
  url "https://github.com/lzfse/lzfse/archive/refs/tags/lzfse-1.0.tar.gz"
  sha256 "cf85f373f09e9177c0b21dbfbb427efaedc02d035d2aade65eb58a3cbf9ad267"
  license "BSD-3-Clause"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "3978fbe7c6d03b15c267fa67614b25d0401e51375828b8349d5bde3555ba61ab"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    args = %W[
      -DCMAKE_INSTALL_RPATH=#{rpath}
    ]
    # Workaround to build with CMake 4
    args << "-DCMAKE_POLICY_VERSION_MINIMUM=3.5"
    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"original").write Random.new.bytes(0xFFFF)

    system bin/"lzfse", "-encode", "-i", "original", "-o", "encoded"
    system bin/"lzfse", "-decode", "-i", "encoded", "-o", "decoded"

    assert_equal (testpath/"original").read, (testpath/"decoded").read
  end
end
