class StaticWebServer < Formula
  desc "High-performance and asynchronous web server for static files-serving"
  homepage "https://static-web-server.net"
  url "https://github.com/static-web-server/static-web-server/releases/download/v2.44.1/static-web-server-v2.44.1-x86_64-pc-windows-gnu.zip"
  sha256 "e96e2c8b1ae4a4beb95a1abcead8bfed2b744429ef61a87e45e01fff98088062"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/static-web-server/static-web-server/archive/refs/tags/v2.44.1.tar.gz"
  end

  def install
    bin.install "static-web-server.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/static-web-server --version 2>&1")
  end
end
