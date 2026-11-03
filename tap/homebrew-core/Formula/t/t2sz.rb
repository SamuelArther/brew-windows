class T2sz < Formula
  desc "Compress a file into a seekable zstd with per-file seeking for tar archives"
  homepage "https://github.com/martinellimarco/t2sz"
  url "https://github.com/martinellimarco/t2sz/archive/refs/tags/v1.3.0.tar.gz"
  sha256 "088b4cc1896f1c85c92db572f0ac170a9231492e244305754ca9bd5287faaa69"
  license "GPL-3.0-or-later"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "b74e86409b17f8e93a148188f211dd246272140d98746a1104b8cfdb1a970f1a"
  end

  depends_on "cmake" => :build
  depends_on "zstd"

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/t2sz -V 2>&1")

    (testpath/"hello.txt").write "Hello, Homebrew!"
    system "tar", "cf", "test.tar", "-C", testpath, "hello.txt"
    system bin/"t2sz", "-o", "test.tar.zst", "test.tar"
    assert_path_exists testpath/"test.tar.zst"

    system "zstd", "-o", "test.restored.tar", "--decompress", "test.tar.zst"
    assert_equal (testpath/"test.tar").read, (testpath/"test.restored.tar").read
  end
end
