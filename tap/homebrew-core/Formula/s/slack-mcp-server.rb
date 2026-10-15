class SlackMcpServer < Formula
  desc "Powerful MCP Slack Server with multiple transports and smart history fetch logic"
  homepage "https://github.com/korotovsky/slack-mcp-server"
  url "https://github.com/korotovsky/slack-mcp-server/releases/download/v1.3.0/slack-mcp-server-windows-amd64.exe"
  sha256 "d07e49dcd83459d1946dc1855529a3891bb788ac42ada83b3710b180251d21a7"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/korotovsky/slack-mcp-server/archive/refs/tags/v1.3.0.tar.gz"
  end

  def install
    bin.install "slack-mcp-server-windows-amd64.exe" => "slack-mcp-server.exe"
  end

  test do
    system bin/"slack-mcp-server", "--help"
  end
end
