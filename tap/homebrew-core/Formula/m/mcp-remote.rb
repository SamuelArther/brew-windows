class McpRemote < Formula
  desc "Remote proxy for Model Context Protocol with OAuth support"
  homepage "https://github.com/geelen/mcp-remote"
  url "https://registry.npmjs.org/mcp-remote/-/mcp-remote-0.16.7.tgz"
  sha256 "5886c7cabf92199e4dfe9aa0f7cc5518d16e7907dc4b3bfcd7882874f9934582"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a15073607b0a3fcdfd6a914b66d35647041246116af9b8738eb413afda0ac817"
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
