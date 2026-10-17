class SvtVp9 < Formula
  desc "Scalable Video Technology for VP9 Encoder"
  homepage "https://github.com/OpenVisualCloud/SVT-VP9"
  url "https://github.com/OpenVisualCloud/SVT-VP9/archive/refs/tags/v0.3.1.tar.gz"
  sha256 "2385fee19ea82c3e6157215ffbe1851fdabd46d1502e4343c842bf7f9bf662cd"
  license "BSD-2-Clause-Patent"
  head "https://github.com/OpenVisualCloud/SVT-VP9.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a1f47c4cbf2b93898c21f1dc2aa7ba08ff1995fdcc46db1c578c3b07910d707c"
  end

  depends_on "cmake" => :build
  depends_on "nasm" => :build

  depends_on arch: :x86_64

  resource "homebrew-testvideo", :test do
    url "https://github.com/grusell/svt-av1-homebrew-testdata/raw/main/video_64x64_yuv420p_25frames.yuv"
    sha256 "0c5cc90b079d0d9c1ded1376357d23a9782a704a83e01731f50ccd162e246492"
  end

  allow_network_access! :test

  def install
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    testpath.install resource("homebrew-testvideo")
    system bin/"SvtVp9EncApp", "-w", "64", "-h", "64", "-i", "video_64x64_yuv420p_25frames.yuv", "-b", "output.ivf"
    assert_path_exists testpath/"output.ivf"
  end
end
