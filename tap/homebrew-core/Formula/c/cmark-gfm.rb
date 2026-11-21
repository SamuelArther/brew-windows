class CmarkGfm < Formula
  desc "C implementation of GitHub Flavored Markdown"
  homepage "https://github.com/github/cmark-gfm"
  url "https://github.com/github/cmark-gfm/archive/refs/tags/0.29.0.gfm.14.tar.gz"
  version "0.29.0.gfm.14"
  sha256 "1c1acf50c5e9939e170b6e017ec06fcf94abeb383670b819d091a285b7ba11fe"
  license "BSD-2-Clause"
  compatibility_version 2

  bottle do
    sha256 cellar: :any, x86_64_windows: "1f06ef6909c74606a25e4f4c26c3e05d40dbd06b93fb2bea952d72e2d5662d68"
  end

  depends_on "cmake" => :build

  uses_from_macos "python" => :build

  deny_network_access!

  def install
    args = %W[
      -DCMAKE_INSTALL_RPATH=#{rpath}
    ]
    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    output = pipe_output("#{bin}/cmark-gfm --extension autolink", "https://brew.sh")
    assert_equal '<p><a href="https://brew.sh">https://brew.sh</a></p>', output.chomp
  end
end
