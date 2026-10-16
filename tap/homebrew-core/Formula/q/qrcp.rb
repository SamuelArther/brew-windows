class Qrcp < Formula
  desc "Transfer files to and from your computer by scanning a QR code"
  homepage "https://qrcp.sh"
  url "https://github.com/claudiodangelis/qrcp/releases/download/v0.11.7/qrcp_0.11.7_windows_amd64.tar.gz"
  sha256 "072082e1bace619f3fedba2b3189d3f6420d32a2dbb87d5831c9b883b95f47f7"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/claudiodangelis/qrcp/archive/refs/tags/v0.11.7.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "qrcp.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/qrcp version")

    data = "Hello there, big world\n"
    port = free_port
    server_url = "http://localhost:#{port}/send/testing"

    (testpath/"test_data.txt").write data
    (testpath/"config.json").write <<~JSON
      {
        "interface": "any",
        "fqdn": "localhost",
        "port": #{port}
      }
    JSON

    spawn bin/"qrcp", "-c", testpath/"config.json", "--path", "testing", testpath/"test_data.txt"
    sleep 1

    # User-Agent header needed in order for curl to be able to receive file
    assert_equal data, shell_output("curl -H \"User-Agent: Mozilla\" #{server_url}")
  end
end
