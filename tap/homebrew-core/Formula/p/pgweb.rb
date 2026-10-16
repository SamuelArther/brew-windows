class Pgweb < Formula
  desc "Web-based PostgreSQL database browser"
  homepage "https://sosedoff.github.io/pgweb/"
  url "https://github.com/sosedoff/pgweb/archive/refs/tags/v0.17.0.tar.gz"
  sha256 "5a79b4a13f313f8b38d63957495bd6ece01ab28cf83e23b288cbb7a1b3dd7cfa"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "231bf92d31ef7dc12c32274b06fcbe75ae0a9650fcfd8bf48d51f28dedd26197"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X github.com/sosedoff/pgweb/pkg/command.BuildTime=#{time.iso8601}
      -X github.com/sosedoff/pgweb/pkg/command.GoVersion=#{Formula["go"].version}
    ]

    system "go", "build", *std_go_args(ldflags:)
  end

  test do
    port = free_port
    pid = spawn bin/"pgweb", "--listen=#{port}", "--skip-open", "--sessions"
    begin
      sleep 2
      assert_match "\"version\":\"#{version}\"", shell_output("curl http://localhost:#{port}/api/info")
    ensure
      Process.kill("TERM", pid)
      Process.wait(pid)
    end
  end
end
