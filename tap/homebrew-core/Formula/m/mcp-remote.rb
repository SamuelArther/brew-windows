class McpRemote < Formula
  desc "Remote proxy for Model Context Protocol with OAuth support"
  homepage "https://github.com/geelen/mcp-remote"
  url "https://registry.npmjs.org/mcp-remote/-/mcp-remote-0.17.1.tgz"
  sha256 "c582f826f1442fc9e7b34440ebb86741e8aeb5a197a239a397bd7204a4b1e1f3"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "25c588104357afc2dcedd07455c6b4ba6a2e22af607071d2307495fcf2b56dd6"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match "Using transport strategy: http-first",
      shell_output("#{bin}/mcp-remote https://mcp.example.com/mcp 2>&1", 1)
  end
end
