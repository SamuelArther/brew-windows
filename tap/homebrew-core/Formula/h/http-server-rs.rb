class HttpServerRs < Formula
  desc "Simple and configurable command-line HTTP server"
  homepage "https://github.com/http-server-rs/http-server"
  url "https://github.com/http-server-rs/http-server/archive/refs/tags/v0.8.9.tar.gz"
  sha256 "b385a979946efb9421f49b698ce9fe1f0e590a96797e72e8e441bebd6ce65bb6"
  license "Apache-2.0"
  head "https://github.com/http-server-rs/http-server.git", branch: "main"

  # Avoid pre-releases like `v1.0.0-pre.20251223080344`
  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "6da6d4e327d806307a7d7965183399c7f77d3cb45c0aa4a267a578326e125de2"
  end

  depends_on "rust" => :build

  conflicts_with "http-server", because: "both install a `http-server` binary"

  # `test do` block runs a local HTTP server.
  allow_network_access! :test

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    touch testpath/"foobar"
    port = free_port
    pid = spawn bin/"http-server", "-q", "-p", port.to_s
    sleep 3
    output = shell_output("curl -s http://localhost:#{port}")
    assert_match "foobar", output
  ensure
    Process.kill("SIGINT", pid)
    Process.wait(pid)
  end
end
