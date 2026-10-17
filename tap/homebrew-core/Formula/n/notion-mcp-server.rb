class NotionMcpServer < Formula
  desc "MCP Server for Notion"
  homepage "https://github.com/makenotion/notion-mcp-server"
  url "https://registry.npmjs.org/@notionhq/notion-mcp-server/-/notion-mcp-server-2.5.2.tgz"
  sha256 "2506106e21e2487531d106cf0b37cd3b1b18913c91dc141719f1c05b91f8bbce"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "df786d761209600c925d127ef63aeedb1ec2f7d0f191880bfbd1533a1c014a29"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    json = <<~JSON
      {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26"}}
      {"jsonrpc":"2.0","id":2,"method":"tools/list"}
    JSON

    assert_match "Identifier for a Notion data source", pipe_output(bin/"notion-mcp-server", json, 0)
  end
end
