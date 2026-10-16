class Base16384 < Formula
  desc "Encode binary files to printable utf16be"
  homepage "https://github.com/fumiama/base16384"
  url "https://github.com/fumiama/base16384/archive/refs/tags/v2.3.2.tar.gz"
  sha256 "3b612e8ab32e7b108a08cdf4112a04fbebaaa572bc60d386343a954c695e450b"
  license "GPL-3.0-or-later"
  head "https://github.com/fumiama/base16384.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f08ff4346d49e2d391817f77d5e4c50b7a560af14174ea7c5b9503b7a5f030fd"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    hash = pipe_output("#{bin}/base16384 -e - -", "1234567890abcdefg", 0)
    assert_match "1234567890abcdefg", pipe_output("#{bin}/base16384 -d - -", hash, 0)
  end
end
