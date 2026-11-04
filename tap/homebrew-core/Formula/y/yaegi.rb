class Yaegi < Formula
  desc "Yet another elegant Go interpreter"
  homepage "https://github.com/traefik/yaegi"
  url "https://github.com/traefik/yaegi/archive/refs/tags/v0.16.2.tar.gz"
  sha256 "1fe741797b37cdef32662f093b3042c1dd4e06fafd086450765dd0a9f4fb0287"
  license "Apache-2.0"
  head "https://github.com/traefik/yaegi.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "830129d1c413ea5c9c769f8568e31a6adc2534a81a55ccc49b8424e010f2a04a"
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
