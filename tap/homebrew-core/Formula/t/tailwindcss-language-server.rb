class TailwindcssLanguageServer < Formula
  desc "LSP for TailwindCSS"
  homepage "https://github.com/tailwindlabs/tailwindcss-intellisense/tree/HEAD/packages/tailwindcss-language-server"
  url "https://github.com/tailwindlabs/tailwindcss-intellisense/archive/refs/tags/v0.16.0.tar.gz"
  sha256 "96329cb80579ca6e81725c9d6510fc3ced568bcc6bef2bdd20de365e6b9f479c"
  license "MIT"

  livecheck do
    url "https://registry.npmjs.org/@tailwindcss/language-server/latest"
    strategy :json do |json|
      json["version"]
    end
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "d9e1cfcf95d8d4e6cb1dd732a970ca00757613e8c421e75d54dfb3c9d7e85c71"
  end

  depends_on "pnpm" => :build
  depends_on "node"

  deny_network_access!

  def fetch
    cd "packages/tailwindcss-language-server" do
      system "pnpm", "with", "current", "fetch", "--ignore-scripts"
    end
  end

  def install
    cd "packages/tailwindcss-language-server" do
      system "pnpm", "--offline", "with", "current", "install", "--frozen-lockfile", "--ignore-scripts"
      system "pnpm", "with", "current", "run", "build"
      bin.install "bin/tailwindcss-language-server"
    end
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

    Open3.popen3(bin/"tailwindcss-language-server", "--stdio") do |stdin, stdout|
      stdin.write "Content-Length: #{json.size}\r\n\r\n#{json}"
      sleep 3
      assert_match(/^Content-Length: \d+/i, stdout.readline)
    end
  end
end
