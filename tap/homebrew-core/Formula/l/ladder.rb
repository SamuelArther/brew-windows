class Ladder < Formula
  desc "Selfhosted alternative to 12ft.io and 1ft.io HTTP web proxies"
  homepage "https://github.com/everywall/ladder"
  url "https://github.com/everywall/ladder/archive/refs/tags/v0.0.23.tar.gz"
  sha256 "e837284c3e283d7de1441b55606bd54ecc10bb6c81abed5a96b9e6836d7d3a2a"
  license "GPL-3.0-only"
  head "https://github.com/everywall/ladder.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "7991b8f9918ff6170daf4674ebea24406ba64013b51e0e282078d341c0c9ee00"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/main.go"
  end

  test do
    port = free_port
    pid = spawn bin/"ladder", "-p", port.to_s
    sleep 2

    output = shell_output("curl -s http://127.0.0.1:#{port}/")
    assert_match "ladder", output
  ensure
    Process.kill("SIGINT", pid)
    Process.wait(pid)
  end
end
