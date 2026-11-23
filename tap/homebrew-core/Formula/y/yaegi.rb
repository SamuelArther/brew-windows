class Yaegi < Formula
  desc "Yet another elegant Go interpreter"
  homepage "https://github.com/traefik/yaegi"
  url "https://github.com/traefik/yaegi/archive/refs/tags/v2.0.0.tar.gz"
  sha256 "ac1e4e7c38a7f2911ca0b7786caaa8998d2ca7f301cfaf3cca536b256af492e1"
  license "Apache-2.0"
  head "https://github.com/traefik/yaegi.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "9eb45dbbe928dcfcaf7d0b7a2f695ee0998cf553207a7efc1c48c06323516926"
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
