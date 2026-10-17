class McpRemote < Formula
  desc "Remote proxy for Model Context Protocol with OAuth support"
  homepage "https://github.com/geelen/mcp-remote"
  url "https://registry.npmjs.org/mcp-remote/-/mcp-remote-0.15.1.tgz"
  sha256 "45aa7ae3c2ad1b51383c1ddce67f6fd26efbbe6637669e8f4597abe37bb87c05"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "04e32072af922fb994c4d019bd99e9960e1a4d2a900a6fc6e62281785048eef9"
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
