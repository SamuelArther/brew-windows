class Yaegi < Formula
  desc "Yet another elegant Go interpreter"
  homepage "https://github.com/traefik/yaegi"
  url "https://github.com/traefik/yaegi/archive/refs/tags/v2.0.1.tar.gz"
  sha256 "5ddd97fa64f9135f2741d2177f07e6ca8a7a52b01413c3ffd3199c27b7a2b64a"
  license "Apache-2.0"
  head "https://github.com/traefik/yaegi.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "fa2e89dea5c1827c26cc75543fab206327c75d94ab703be287f64c163ba46d57"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.version=#{version}"), "./cmd/yaegi"
  end

  test do
    assert_match "4", pipe_output("#{bin}/yaegi -i", "3 + 1", 0)
  end
end
