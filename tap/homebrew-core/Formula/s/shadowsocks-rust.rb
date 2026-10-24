class ShadowsocksRust < Formula
  desc "Rust port of Shadowsocks"
  homepage "https://shadowsocks.org/"
  url "https://github.com/shadowsocks/shadowsocks-rust/archive/refs/tags/v1.25.0.tar.gz"
  sha256 "477a525e9b11d4c90ded6629552548d00a32675ec6f6f18ee45c98447e54b5f3"
  license "MIT"
  head "https://github.com/shadowsocks/shadowsocks-rust.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "1f02b1ef88f820a06864e8810c26c0445ba5a0ceb0507635e71a4c20cd5658de"
  end

  depends_on "rust" => :build

  allow_network_access! :test

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    server_port = free_port
    local_port = free_port

    (testpath/"server.json").write <<~JSON
      {
          "server":"127.0.0.1",
          "server_port":#{server_port},
          "password":"mypassword",
          "method":"aes-256-gcm"
      }
    JSON
    (testpath/"local.json").write <<~JSON
      {
          "server":"127.0.0.1",
          "server_port":#{server_port},
          "password":"mypassword",
          "method":"aes-256-gcm",
          "local_address":"127.0.0.1",
          "local_port":#{local_port}
      }
    JSON
    spawn bin/"ssserver", "-c", testpath/"server.json"
    spawn bin/"sslocal", "-c", testpath/"local.json"
    sleep 3

    output = shell_output "curl --socks5 127.0.0.1:#{local_port} https://example.com"
    assert_match "Example Domain", output
  end
end
