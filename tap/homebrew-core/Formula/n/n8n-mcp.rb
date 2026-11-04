class N8nMcp < Formula
  desc "MCP for Claude Desktop, Claude Code, Windsurf, Cursor to build n8n workflows"
  homepage "https://www.n8n-mcp.com/"
  url "https://registry.npmjs.org/n8n-mcp/-/n8n-mcp-2.100.0.tgz"
  sha256 "502b8b9a6b862a170a7411a6e90a4823075a31ee81c591dc6076f18bce598c3e"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "612ad67659f50f8c3fbb1f57e025cc90db68cede5b8e3321cd4f1c76cbfec212"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    json = [
      %Q({"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"homebrew","version":"#{version}"}}}),
      '{"jsonrpc":"2.0","method":"notifications/initialized","params":{}}',
      '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}',
    ].join("\n") + "\n"

    output = pipe_output(bin/"n8n-mcp", json, 0)
    assert_match "\"name\":\"n8n-documentation-mcp\"", output
    assert_match "\"name\":\"search_nodes\"", output
  end
end
