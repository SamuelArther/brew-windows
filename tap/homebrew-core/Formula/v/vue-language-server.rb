class VueLanguageServer < Formula
  desc "Vue.js language server"
  homepage "https://deepwiki.com/vuejs/language-tools"
  url "https://registry.npmjs.org/@vue/language-server/-/language-server-3.3.13.tgz"
  sha256 "39013d18a4e639db5988ebabfb2ff49393940c6cc492731dc54c5b8fde6b24de"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "afa9b01e31f6896ca608721a902743a71d6b0545e335ffdb68ebe856643228b7"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    require "open3"

    json = <<~JSON
      {
        "jsonrpc": "2.0",
        "id": 1,
        "method": "initialize",
        "params": {
          "rootUri": null,
          "capabilities": {}
        }
      }
    JSON

    Open3.popen3(bin/"vue-language-server", "--stdio") do |stdin, stdout|
      stdin.write "Content-Length: #{json.size}\r\n\r\n#{json}"
      sleep 3
      assert_match(/^Content-Length: \d+/i, stdout.readline)
    end
  end
end
