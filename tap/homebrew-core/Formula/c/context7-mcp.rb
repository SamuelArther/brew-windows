class Context7Mcp < Formula
  desc "Up-to-date code documentation for LLMs and AI code editors"
  homepage "https://context7.com"
  url "https://registry.npmjs.org/@upstash/context7-mcp/-/context7-mcp-4.5.1.tgz"
  sha256 "aa99756db9b4e8205920e5f52715618318edadf397cf658b05793b0acee76bf1"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "9131b55e6623edc03d460b61caaccf47c1a589a6ae69fdbba122610c2e3bc78e"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    json = <<~JSON
      {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26"}}
      {"jsonrpc":"2.0","id":2,"method":"tools/list"}
    JSON
    output = pipe_output(bin/"context7-mcp", json, 0)
    assert_match "resolve-library-id", output
  end
end
