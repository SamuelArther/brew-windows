class McpRemote < Formula
  desc "Remote proxy for Model Context Protocol with OAuth support"
  homepage "https://github.com/geelen/mcp-remote"
  url "https://registry.npmjs.org/mcp-remote/-/mcp-remote-0.15.2.tgz"
  sha256 "364a418993680ee0385baafe807f4f74ce72a4d6165dfe96c2d39dd08a8d74ff"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "3686c793afdc54e962e40397a1e6cde1135687f993abbd0af4dd5d5a2927c30b"
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
