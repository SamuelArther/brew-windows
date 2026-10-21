class McpGrafana < Formula
  desc "MCP server for Grafana"
  homepage "https://github.com/grafana/mcp-grafana"
  url "https://github.com/grafana/mcp-grafana/archive/refs/tags/v2.1.1.tar.gz"
  sha256 "57cace46d2cca79de1a34e6d43393bfba7afc92f7a93ecf5672d747f8ba3cc66"
  license "Apache-2.0"
  head "https://github.com/grafana/mcp-grafana.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f5807a41b2d9d5a4f6a2e335be06cd3744c30487a352061b1821e22d9f454098"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: :goreleaser), "./cmd/mcp-grafana"
  end

  test do
    IO.popen([bin/"mcp-grafana", "--usage-stats=disabled"], "r+") do |pipe|
      Timeout.timeout(30) do
        pipe.puts JSON.generate(jsonrpc: "2.0", id: 1, method: "initialize", params: {
          protocolVersion: "2025-03-26", capabilities: {}, clientInfo: { name: "homebrew", version: "1.0" }
        })
        response = JSON.parse(pipe.readline)
        assert_equal 1, response.fetch("id")
        assert_match "This server provides access to your Grafana instance and the surrounding ecosystem",
                     response.fetch("result").fetch("instructions")

        pipe.puts JSON.generate(jsonrpc: "2.0", method: "notifications/initialized")
        pipe.puts JSON.generate(jsonrpc: "2.0", id: 2, method: "tools/list")
        response = JSON.parse(pipe.readline)
        assert_equal 2, response.fetch("id")
        tools = response.fetch("result").fetch("tools").map { |tool| tool.fetch("name") }
        assert_includes tools, "list_datasources"
        assert_includes tools, "query_prometheus"
      end
    end
  end
end
