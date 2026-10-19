class Portop < Formula
  desc "Terminal UI showing which processes are using TCP and UDP ports"
  homepage "https://github.com/padovanl/portop"
  url "https://github.com/padovanl/portop/archive/refs/tags/v0.3.0.tar.gz"
  sha256 "e84d56316ae48ebef064d1f8e995885e93ecd2d4d3abb3ba0e8e8bfec91b939a"
  license "MIT"
  head "https://github.com/padovanl/portop.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "cd58ac49d9946062659ed2f35c14c2327ce9c59901812a3e0d07d6b31a33cd26"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X github.com/padovanl/portop/internal/cli.Version=#{version}"
    system "go", "build", *std_go_args(ldflags:), "./cmd/portop"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/portop --version")
    assert_kind_of Array, JSON.parse(shell_output("#{bin}/portop --json"))
  end
end
