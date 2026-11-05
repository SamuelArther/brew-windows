class Yaegi < Formula
  desc "Yet another elegant Go interpreter"
  homepage "https://github.com/traefik/yaegi"
  url "https://github.com/traefik/yaegi/archive/refs/tags/v0.16.3.tar.gz"
  sha256 "1fca3442639e2f25fffc6d6cf104dd33a9c0951d32bf8b958ed6e2e979d32d1d"
  license "Apache-2.0"
  head "https://github.com/traefik/yaegi.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "be1fb174389c9ad4c3f06467a864413fd1ba55946b3382b0ea5812b39fc5a829"
  end

  depends_on "go" => :build

  deny_network_access!

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.version=#{version}"), "./cmd/yaegi"
  end

  test do
    assert_match "4", pipe_output(bin/"yaegi", "println(3 + 1)", 0)
  end
end
