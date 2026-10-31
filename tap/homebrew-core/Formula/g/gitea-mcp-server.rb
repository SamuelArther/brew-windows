class GiteaMcpServer < Formula
  desc "Interactive with Gitea instances with MCP"
  homepage "https://gitea.com/gitea/gitea-mcp"
  url "https://gitea.com/gitea/gitea-mcp/archive/v1.12.0.tar.gz"
  sha256 "4474a6879175e0d39bae98cb0b3feb4b0cac6e86724b30bf977b0a14d7e70dbf"
  license "MIT"
  head "https://gitea.com/gitea/gitea-mcp.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "733c45b4200426bf1a36969f343825f1c91fed6a2204184764308226b9fc2e4a"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.version=#{version}")
  end

  test do
    json = <<~JSON
      {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26"}}
      {"jsonrpc":"2.0","id":2,"method":"tools/list"}
    JSON

    # Read the reply before closing stdin: 1.7.0 exits non-zero on EOF without flushing
    output = IO.popen("#{bin}/gitea-mcp-server stdio", "r+") do |io|
      io.write json
      io.readline
    end
    assert_match "Gitea MCP Server", output
  end
end
