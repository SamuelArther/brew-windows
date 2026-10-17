class HttpServer < Formula
  desc "Simple zero-configuration command-line HTTP server"
  homepage "https://github.com/http-party/http-server"
  url "https://registry.npmjs.org/http-server/-/http-server-14.1.1.tgz"
  sha256 "9e1ceb265d09a4d86dcf509cb4ba6dcd2e03254b1d13030198766fe3897fd7a5"
  license "MIT"
  head "https://github.com/http-party/http-server.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "7a2233cf4fa4f191c59aaab893308cd08d0ca39e8f56c8470715fcf70851dc21"
  end

  depends_on "node"

  conflicts_with "http-server-rs", because: "both install a `http-server` binary"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    port = free_port

    pid = spawn bin/"http-server", "-p#{port}"
    sleep 10
    output = shell_output("curl -sI http://localhost:#{port}")
    assert_match "200 OK", output
  ensure
    Process.kill("HUP", pid)
  end
end
