class Protozero < Formula
  desc "Minimalist protocol buffer decoder and encoder in C++"
  homepage "https://github.com/mapbox/protozero"
  url "https://github.com/mapbox/protozero/archive/refs/tags/v1.8.2.tar.gz"
  sha256 "68d1df951b4d8397355c8fc9ffed40d9c06282a877fe847403feac6edcee3ef1"
  license "BSD-2-Clause"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/protozero-1.8.2"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "2d92b3249690de2652e790651a6816821d32679f45e10bdc2785db767ab8dd6c"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    # We only install headers, so we can skip `cmake --build`.
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--install", "build"
    pkgshare.install "tools"
  end

  test do
    system ENV.cxx, "-std=c++14", "-I#{include}", pkgshare/"tools/pbf-decoder.cpp", "-o", "pbf-decoder"
    assert_empty pipe_output("./pbf-decoder -", "")
  end
end
