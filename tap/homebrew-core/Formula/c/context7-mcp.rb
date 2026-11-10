class Context7Mcp < Formula
  desc "Up-to-date code documentation for LLMs and AI code editors"
  homepage "https://context7.com"
  url "https://registry.npmjs.org/@upstash/context7-mcp/-/context7-mcp-4.6.0.tgz"
  sha256 "20e9e401baa80ee5a9f9d77e5d6b5e5897fbaa89118b5db9003202396d19a988"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "3beca30415c5790564d1345a738cce19e567a5cf5eb0c45f397c541ec772ea59"
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
