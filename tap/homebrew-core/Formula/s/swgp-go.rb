class SwgpGo < Formula
  desc "Simple WireGuard proxy with minimal overhead for WireGuard traffic"
  homepage "https://github.com/database64128/swgp-go"
  url "https://github.com/database64128/swgp-go/archive/refs/tags/v1.10.0.tar.gz"
  sha256 "0d7e5ebce9c07237e71028eac31b27c51d155c5a72eefc7bceac4cc816725d23"
  license "AGPL-3.0-or-later"
  head "https://github.com/database64128/swgp-go.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "1effa4a5c9033299712be28625dbfde03e567f48cf2cb108e5ab640d247fd3a8"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/swgp-go"
  end

  test do
    wg_server_port = free_port
    wg_client_port = free_port
    listen_port = free_port
    (testpath/"server.json").write <<~JSON
      {
        "servers": [
          {
            "name": "server",
            "proxyListen": ":#{listen_port}",
            "proxyMode": "zero-overhead-2026",
            "proxyPSK": "sAe5RvzLJ3Q0Ll88QRM1N01dYk83Q4y0rXMP1i4rDmI=",
            "proxyFwmark": 0,
            "wgEndpoint": "[::1]:#{wg_server_port}",
            "wgFwmark": 0,
            "mtu": 1500
          }
        ]
      }
    JSON
    server = spawn bin/"swgp-go", "-confPath", testpath/"server.json"

    (testpath/"client.json").write <<~JSON
      {
        "clients": [
          {
            "name": "client",
            "wgListen": ":#{wg_client_port}",
            "wgFwmark": 0,
            "proxyEndpoint": "[::1]:#{listen_port}",
            "proxyMode": "zero-overhead-2026",
            "proxyPSK": "sAe5RvzLJ3Q0Ll88QRM1N01dYk83Q4y0rXMP1i4rDmI=",
            "proxyFwmark": 0,
            "mtu": 1500
          }
        ]
      }
    JSON
    client = spawn bin/"swgp-go", "-confPath", testpath/"client.json"

    sleep 3
  ensure
    Process.kill "TERM", server
    Process.kill "TERM", client
    Process.wait server
    Process.wait client
  end
end
