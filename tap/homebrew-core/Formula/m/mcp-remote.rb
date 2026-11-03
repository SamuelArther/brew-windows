class McpRemote < Formula
  desc "Remote proxy for Model Context Protocol with OAuth support"
  homepage "https://github.com/geelen/mcp-remote"
  url "https://registry.npmjs.org/mcp-remote/-/mcp-remote-0.16.0.tgz"
  sha256 "72564c80fcfe6e15c459900ec796a7dc9ed7887f2c915076e3bd050e62ed27ea"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "242cf8f3f0b63446a38601963cc9772776a356ca9a13abc1148611d163b2c4e6"
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
