class Shell2http < Formula
  desc "Executing shell commands via HTTP server"
  homepage "https://github.com/msoap/shell2http"
  url "https://github.com/msoap/shell2http/releases/download/v1.18.0/shell2http_1.18.0_windows_amd64.zip"
  sha256 "47b6e20f6cc1c76746675ad9e60570f11b20b251cf026d4d3efbfd0091aa7f9d"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/msoap/shell2http/archive/refs/tags/v1.18.0.tar.gz"
  end

  def install
    bin.install "shell2http.exe"
  end

  test do
    port = free_port
    pid = spawn bin/"shell2http", "-port", port.to_s, "/echo", "echo brewtest"
    sleep 1
    output = shell_output("curl -s http://localhost:#{port}")
    assert_match "Served by shell2http/#{version}", output

    output = shell_output("curl -s http://localhost:#{port}/echo")
    assert_match "brewtest", output
  ensure
    Process.kill("TERM", pid)
    Process.wait(pid)
  end
end
