class CmuPocketsphinx < Formula
  desc "Lightweight speech recognition engine for mobile devices"
  homepage "https://cmusphinx.github.io/"
  url "https://github.com/cmusphinx/pocketsphinx/archive/refs/tags/v5.1.1.tar.gz"
  sha256 "e2db414eb66618cd0a98de77507db32517a48f6900b06bfb94c0acc4bef5761d"
  license "BSD-2-Clause"
  head "https://github.com/cmusphinx/pocketsphinx.git", branch: "main"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/cmu-pocketsphinx-5.1.1"
    sha256 cellar: :any, x86_64_windows: "5f98f6d4eb85acaede5ed7723d615fe6ed15b49169196c09ce9d86ce92d6a18a"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args,
                                              "-DBUILD_SHARED_LIBS=ON",
                                              "-DCMAKE_INSTALL_RPATH=#{rpath}"
    system "cmake", "--build", "build"
    system "cmake", "--build", "build", "--target", "install"
  end

  test do
    output = shell_output("#{bin}/pocketsphinx single #{test_fixtures("test.mp3")} 2>&1")
    assert_match "{\"b\":0.000,\"d\":0.020,\"p\":1.000,\"t\":\"\",\"w\":[]}", output
  end
end
