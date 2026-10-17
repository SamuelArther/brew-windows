class Vvdec < Formula
  desc "Fraunhofer Versatile Video Decoder"
  homepage "https://www.hhi.fraunhofer.de/en/departments/vca/technologies-and-solutions/h266-vvc.html"
  url "https://github.com/fraunhoferhhi/vvdec/archive/refs/tags/v3.2.1.tar.gz"
  sha256 "5c334557a33cd93e981b84ba0e77126ef970a38e2481b67f0475db40f75379b8"
  license "BSD-3-Clause-Clear"
  head "https://github.com/fraunhoferhhi/vvdec.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "fd16c34986dc53214ff21ec122f2f70aa72455e4fea444b5c3f548cf07fd9f6c"
  end

  depends_on "cmake" => :build

  resource "homebrew-test-video", :test do
    url "https://archive.org/download/testvideo_20230410_202304/test.vvc"
    sha256 "753261009b6472758cde0dee2c004ff712823b43e62ec3734f0f46380bec8e46"
  end

  allow_network_access! :test

  def install
    # SIMD implementations behind the per-source `-march` flags are chosen at runtime.
    ENV.runtime_cpu_detection

    system "cmake", "-S", ".", "-B", "build",
           "-DBUILD_SHARED_LIBS=1",
           "-DVVDEC_INSTALL_VVDECAPP=1",
           *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    resource("homebrew-test-video").stage testpath
    system bin/"vvdecapp", "-b", testpath/"test.vvc", "-o", testpath/"test.yuv"
    assert_path_exists testpath/"test.yuv"
  end
end
