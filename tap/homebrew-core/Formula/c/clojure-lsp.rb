class ClojureLsp < Formula
  desc "Language Server (LSP) for Clojure"
  homepage "https://clojure-lsp.io"
  url "https://github.com/clojure-lsp/clojure-lsp/releases/download/2026.10.15-11.42.38/clojure-lsp-standalone.jar"
  version "2026.10.15-11.42.38"
  sha256 "b6d4d54b1b53beaf771b791f8d9f8cfea6bb613ae0df337089e6d738718e179b"
  license "MIT"
  version_scheme 1
  head "https://github.com/clojure-lsp/clojure-lsp.git", branch: "master"

  livecheck do
    url :stable
    regex(/^v?(\d{4}(?:[.-]\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "d34fc21b029a22d092fdb9cb6a793e61db0b64a0cde418923e4d4322b64d1562"
  end

  depends_on "openjdk"

  def install
    libexec.install "clojure-lsp-standalone.jar"
    bin.write_jar_script libexec/"clojure-lsp-standalone.jar", "clojure-lsp"
  end

  test do
    require "open3"

    json = <<~JSON
      {
        "jsonrpc": "2.0",
        "id": 1,
        "method": "initialize",
        "params": {
          "processId": 88075,
          "rootUri": null,
          "capabilities": {},
          "trace": "verbose",
          "workspaceFolders": null
        }
      }
    JSON

    Open3.popen3(bin/"clojure-lsp") do |stdin, stdout|
      stdin.write "Content-Length: #{json.size}\r\n\r\n#{json}"
      assert_match(/^Content-Length: \d+/i, stdout.readline)
    end
  end
end
