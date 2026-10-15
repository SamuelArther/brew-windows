class DomainCheck < Formula
  desc "CLI tool for checking domain availability using RDAP and WHOIS protocols"
  homepage "https://github.com/saidutt46/domain-check"
  url "https://github.com/saidutt46/domain-check/releases/download/v1.2.0/domain-check-mcp-windows-x86_64.exe.zip"
  sha256 "eddc432251f2aaafa89eaf12d8482169e9e6bcf2511478b6842efe9d39e10660"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/saidutt46/domain-check/archive/refs/tags/v1.2.0.tar.gz"
  end

  def install
    bin.install "domain-check-mcp.exe"
  end

  test do
    assert_path_exists bin/"domain-check-mcp.exe"
  end
end
