class Mubeng < Formula
  desc "Incredibly fast proxy checker & IP rotator with ease"
  homepage "https://github.com/mubeng/mubeng"
  url "https://github.com/mubeng/mubeng/archive/refs/tags/v0.23.0.tar.gz"
  sha256 "57a4e10f25ec5e70fafc97cb21a13ffffdc63d3b8004055318ea0ecd1ab63507"
  license "Apache-2.0"
  head "https://github.com/mubeng/mubeng.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "67c8e44631d3d21e8a79b0030e93e2463aefa5dbbd238be00bafa89a39df9935"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-s -w -X github.com/mubeng/mubeng/common.Version=v#{version}"
    system "go", "build", *std_go_args(ldflags:)
  end

  test do
    assert_match "has no valid proxy URLs", pipe_output("#{bin}/mubeng 2>&1", "", 1)

    assert_match version.to_s, shell_output("#{bin}/mubeng --version", 1)
  end
end
