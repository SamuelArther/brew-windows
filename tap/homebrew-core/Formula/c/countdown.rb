class Countdown < Formula
  desc "Terminal countdown timer"
  homepage "https://github.com/antonmedv/countdown"
  url "https://github.com/antonmedv/countdown/archive/refs/tags/v1.5.0.tar.gz"
  sha256 "ac83ec593674a367913413796e8708680cbb6504c8f68ce17152d800a92ccf3b"
  license "MIT"
  head "https://github.com/antonmedv/countdown.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e3c6f39302fdf509ad0665f5e13bc944be65effd564ec2e2ea7840a0986c83ae"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    pipe_output bin/"countdown", "0m0s"
  end
end
