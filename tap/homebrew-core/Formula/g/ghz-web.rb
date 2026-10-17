class GhzWeb < Formula
  desc "Web interface for ghz"
  homepage "https://ghz.sh"
  url "https://github.com/bojand/ghz/archive/refs/tags/v0.121.0.tar.gz"
  sha256 "d92ed00a2cd1be3b14fe680e1615e9dace4fd4cf6d679811173ae7f2611ad762"
  license "Apache-2.0"
  head "https://github.com/bojand/ghz.git", branch: "master"

  livecheck do
    formula "ghz"
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "8a8f8e4441e05fe82d096b2750c05ff88c97499a11a687dc94e3dc8350580318"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ENV["CGO_ENABLED"] = "1" if OS.linux? && Hardware::CPU.arm?

    system "go", "build", *std_go_args(ldflags: "-X main.version=#{version}"), "./cmd/ghz-web"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ghz-web -v 2>&1")
    port = free_port
    ENV["GHZ_SERVER_PORT"] = port.to_s
    spawn bin/"ghz-web"
    sleep 1
    cmd = "curl -sIm3 -XGET http://localhost:#{port}/"
    assert_match "200 OK", shell_output(cmd)
  end
end
