class NatsStreamingServer < Formula
  desc "Lightweight cloud messaging system"
  homepage "https://nats.io"
  url "https://github.com/nats-io/nats-streaming-server/archive/refs/tags/v0.25.6.tar.gz"
  sha256 "6f53792784e909870c04441127ca855b6d4cf007ccb93d8884d3278fd23b74cf"
  license "Apache-2.0"
  head "https://github.com/nats-io/nats-streaming-server.git", branch: "main"

  deprecate! date: "2026-02-17", because: :repo_archived
  disable! date: "2027-02-17", because: :repo_archived, replacement_formula: "nats-server" # built-in JetStream

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c545353d7eb1533d9ef385df7e762b208a763d97c4a08555e4a682219ba5743c"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  service do
    run opt_bin/"nats-streaming-server"
  end

  test do
    port = free_port
    http_port = free_port
    pid = spawn bin/"nats-streaming-server",
                "--port=#{port}",
                "--http_port=#{http_port}",
                "--pid=#{testpath}/pid",
                "--log=#{testpath}/log"

    begin
      sleep 3
      assert_match "uptime", shell_output("curl localhost:#{http_port}/varz")
      assert_path_exists testpath/"log"
      assert_match version.to_s, (testpath/"log").read
    ensure
      Process.kill "SIGINT", pid
      Process.wait pid
    end
  end
end
