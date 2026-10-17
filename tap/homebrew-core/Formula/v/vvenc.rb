class Vvenc < Formula
  desc "Fraunhofer Versatile Video Encoder"
  homepage "https://www.hhi.fraunhofer.de/en/departments/vca/technologies-and-solutions/h266-vvc.html"
  url "https://github.com/fraunhoferhhi/vvenc/archive/refs/tags/v1.14.0.tar.gz"
  sha256 "dd43d061d59dbc0d9b9ae5b99cb40672877dd811646228938f065798939ee174"
  license "BSD-3-Clause-Clear"
  head "https://github.com/fraunhoferhhi/vvenc.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "6a5aa5b556e6c73e4606ca78456de18534ec2d385f4facfb3257435a17f089e1"
  end

  depends_on "cmake" => :build

  resource "homebrew-test_video", :test do
    url "https://raw.githubusercontent.com/fraunhoferhhi/vvenc/master/test/data/RTn23_80x44p15_f15.yuv"
    sha256 "ecd2ef466dd2975f4facc889e0ca128a6bea6645df61493a96d8e7763b6f3ae9"
  end

  allow_network_access! :test

  def install
    system "cmake", "-S", ".", "-B", "build",
           "-DVVENC_INSTALL_FULLFEATURE_APP=1",
           "-DBUILD_SHARED_LIBS=1",
           *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    resource("homebrew-test_video").stage testpath
    system bin/"vvencapp",
           "-i", testpath/"RTn23_80x44p15_f15.yuv",
           "-s", "360x640",
           "--fps", "60/1",
           "--format", "yuv420_10",
           "--hdr", "hdr10_2020",
           "-o", testpath/"RTn23_80x44p15_f15.vvc"
    assert_path_exists testpath/"RTn23_80x44p15_f15.vvc"
  end
end
