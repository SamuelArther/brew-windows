class McpRemote < Formula
  desc "Remote proxy for Model Context Protocol with OAuth support"
  homepage "https://github.com/geelen/mcp-remote"
  url "https://registry.npmjs.org/mcp-remote/-/mcp-remote-0.16.1.tgz"
  sha256 "e70863727f02fd69e9b0149400e4e10c51c4c148c39c95a216b1f238f93ee051"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "962cd64ec0e79f71ec25b766bee2435a228357a265992dd74363bb258b0aa385"
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
