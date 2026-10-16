class AwsEsProxy < Formula
  desc "Small proxy between HTTP client and AWS Elasticsearch"
  homepage "https://github.com/abutaha/aws-es-proxy"
  url "https://github.com/abutaha/aws-es-proxy/releases/download/v1.5/aws-es-proxy-1.5-windows-amd64.exe"
  sha256 "6d28c0d5fb617c6ec0382c983bfdf22a02546e6651c45824d986ca5702735703"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/abutaha/aws-es-proxy/archive/refs/tags/v1.5.tar.gz"
  end

  def install
    bin.install "aws-es-proxy-1.5-windows-amd64.exe" => "aws-es-proxy.exe"
  end

  test do
    address = "127.0.0.1:#{free_port}"
    endpoint = "https://dummy-host.eu-west-1.es.amazonaws.com"

    pid = spawn bin/"aws-es-proxy", "-listen=#{address}", "-endpoint=#{endpoint}"
    begin
      sleep 2
      output = shell_output("curl --silent #{address}")
      assert_match "Failed to sign", output
    ensure
      Process.kill("TERM", pid)
      Process.wait(pid)
    end
  end
end
