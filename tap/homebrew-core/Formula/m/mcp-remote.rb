class McpRemote < Formula
  desc "Remote proxy for Model Context Protocol with OAuth support"
  homepage "https://github.com/geelen/mcp-remote"
  url "https://registry.npmjs.org/mcp-remote/-/mcp-remote-0.16.6.tgz"
  sha256 "9b701b6522d3a513db99eeb7fc4f04cb261fbb08487645209482eef4408a529e"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "b8f4b5a7f2aded8560cc9bfcfcbd1f7d9df585c098fc49ab3a5a72693fd0ac91"
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
