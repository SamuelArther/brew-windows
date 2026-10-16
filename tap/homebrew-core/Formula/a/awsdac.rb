class Awsdac < Formula
  desc "CLI tool for drawing AWS architecture"
  homepage "https://github.com/awslabs/diagram-as-code"
  url "https://github.com/awslabs/diagram-as-code/releases/download/v0.24/awsdac-mcp-server-v0.24_windows-amd64.zip"
  sha256 "bc62cdcff2aec6f1dcc022570809bace0d8e541760743105cfa4dc56697f3280"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/awslabs/diagram-as-code/archive/refs/tags/v0.24.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/awsdac-mcp-server-v0.24_windows-amd64/awsdac-mcp-server.exe", "awsdac-mcp-server"
  end

  test do
    assert_path_exists bin/"awsdac-mcp-server.exe"
  end
end
