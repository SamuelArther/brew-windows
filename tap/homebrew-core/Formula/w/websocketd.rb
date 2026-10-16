class Websocketd < Formula
  desc "WebSockets the Unix way"
  homepage "http://websocketd.com"
  url "https://github.com/joewalnes/websocketd/releases/download/v0.4.1/websocketd-0.4.1-windows_amd64.zip"
  sha256 "ae73e3711dfe2db54af04f2458ea6a397e5ffc566aaae5ab59e3bf1eaac1d555"
  license "BSD-2-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/joewalnes/websocketd/archive/refs/tags/v0.4.1.tar.gz"
  end

  def install
    bin.install "websocketd.exe"
  end

  test do
    port = free_port
    pid = spawn bin/"websocketd", "--port=#{port}", "echo", "ok"
    begin
      sleep 2
      assert_equal("404 page not found\n", shell_output("curl -s http://localhost:#{port}"))
    ensure
      Process.kill "TERM", pid
      Process.wait pid
    end
  end
end
