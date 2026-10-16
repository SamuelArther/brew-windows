class Zoro < Formula
  desc "Expose local server to external network"
  homepage "https://www.txthinking.com"
  url "https://github.com/txthinking/zoro/archive/refs/tags/v20240828.tar.gz"
  sha256 "8b41550a1d42fa2c0a67d7115978efff126ab6fff30d774ce902febd0b682c5c"
  license "GPL-3.0-only"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f41134bcac44cb4b0de9a7685f37a1701d28af756e2c809d95334eeb8e53b482"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cli/zoro"
  end

  test do
    (testpath/"index.html").write <<~HTML
      <!DOCTYPE HTML>
      <html>
      <body>
        <p>passed</p>
      </body>
      </html>
    HTML
    zoro_server_port = free_port
    server_port = free_port
    client_port = free_port
    server_pid = spawn bin/"zoro", "server", "-l", ":#{zoro_server_port}", "-p", "password"
    sleep 5
    client_pid = spawn bin/"zoro", "client", "-s", "127.0.0.1:#{zoro_server_port}",
                                             "-p", "password",
                                             "--serverport", server_port.to_s,
                                             "--dir", testpath,
                                             "--dirport", client_port.to_s
    sleep 3
    output = shell_output "curl 127.0.0.1:#{server_port}"
    assert_match "passed", output
  ensure
    Process.kill "SIGTERM", server_pid, client_pid
  end
end
