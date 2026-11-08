class McpRemote < Formula
  desc "Remote proxy for Model Context Protocol with OAuth support"
  homepage "https://github.com/geelen/mcp-remote"
  url "https://registry.npmjs.org/mcp-remote/-/mcp-remote-0.16.5.tgz"
  sha256 "82ef9cc8c2aca21f60a7551374315d281cf428b4389d7f179fb5893ddf425b2b"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "dd6efa4c83a686d2362751bb05cf2ff0da1fa5e3cef161d5d122adb730c13aa8"
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
