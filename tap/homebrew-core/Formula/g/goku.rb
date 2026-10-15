class Goku < Formula
  desc "HTTP load testing tool"
  homepage "https://goku.observabilityinsight.com/"
  url "https://github.com/jcaromiq/goku/releases/download/v3.0.0/goku-mcp-Windows-msvc-x86_64.zip"
  sha256 "2eb6cda343030e5fa463439438181f5f2cdb233a2bcdb3709a6c315c14a42aa4"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/jcaromiq/goku/archive/refs/tags/v3.0.0.tar.gz"
  end

  def install
    bin.install "goku-mcp.exe"
  end

  test do
    assert_path_exists bin/"goku-mcp.exe"
  end
end
