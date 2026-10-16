class DdnsGo < Formula
  desc "Simple and easy-to-use DDNS"
  homepage "https://github.com/jeessy2/ddns-go"
  url "https://github.com/jeessy2/ddns-go/releases/download/v6.17.7/ddns-go_6.17.7_windows_x86_64.zip"
  sha256 "6cdc846cdf8d5a081cfab5bf3f8872961044be33c52b316e49dd993a1c6e7b3e"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/jeessy2/ddns-go/archive/refs/tags/v6.17.7.tar.gz"
  end

  def install
    bin.install "ddns-go.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ddns-go -v")

    port = free_port
    spawn "#{bin}/ddns-go -l :#{port} -c #{testpath}/ddns-go.yaml"
    sleep 1

    system "curl", "--silent", "localhost:#{port}/clearLog"
    output = shell_output("curl --silent localhost:#{port}/logs")
    assert_match "Temporary Redirect", output
  end
end
