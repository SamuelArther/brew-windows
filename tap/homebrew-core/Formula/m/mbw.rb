class Mbw < Formula
  desc "Memory Bandwidth Benchmark"
  homepage "https://github.com/raas/mbw/"
  url "https://github.com/raas/mbw/archive/refs/tags/v2.0.tar.gz"
  sha256 "557f670e13ff663086fe239e4184d8ca6154b004bd5fde2b0a748e5aa543c87f"
  license "GPL-3.0-or-later"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "3bf9ecdc95242632c4e9c378f8f401e38091e33aecfa8667fa423bedea591fad"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    assert_match "AVG\tMethod: MEMCPY\tElapsed", pipe_output("#{bin}/mbw 8", "0")
  end
end
