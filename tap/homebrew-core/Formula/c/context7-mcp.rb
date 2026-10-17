class Context7Mcp < Formula
  desc "Up-to-date code documentation for LLMs and AI code editors"
  homepage "https://context7.com"
  url "https://registry.npmjs.org/@upstash/context7-mcp/-/context7-mcp-4.4.2.tgz"
  sha256 "aaaa96e8b6fbeb63f496ef560f0dd96c6774a6a97580378d53ab63ccc5850d2e"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "27cb2ed44b5b641b1dcc20a36e7980af7aca63df2f6ea6133a394b2136080647"
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
