class Serve < Formula
  desc "Static http server anywhere you need one"
  homepage "https://github.com/syntaqx/serve"
  url "https://github.com/syntaqx/serve/archive/refs/tags/v0.8.0.tar.gz"
  sha256 "636223c5b9d9af83601ad82be5dd8788bd35b58160f4420a899e00fc82e7618d"
  license "MIT"
  head "https://github.com/syntaqx/serve.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "715a59ad9b89870bb7794f0429dda570469c9832b44f78aa50afd052ed0ac29e"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.version=#{version}"), "./cmd/serve"
  end

  test do
    (testpath/"index.html").write("<h1>serve</h1>")
    port = free_port
    pid = spawn bin/"serve", "-port", port.to_s
    sleep 1
    output = shell_output("curl -sI http://localhost:#{port}")
    assert_match(/200 OK/m, output)
  ensure
    Process.kill("HUP", pid)
  end
end
