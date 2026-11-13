class Context7Mcp < Formula
  desc "Up-to-date code documentation for LLMs and AI code editors"
  homepage "https://context7.com"
  url "https://registry.npmjs.org/@upstash/context7-mcp/-/context7-mcp-4.7.0.tgz"
  sha256 "35b5a51ab0db7ff66f3b391f2bd97cd38d60803dce584d38d97ea0b99fab7734"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "397b7d0b687a8a383d8fd16ecec67b7be85b570fbf25fceeddac3b5b8e4f0cce"
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
