class ActionsLanguageserver < Formula
  desc "Language server for GitHub Actions YAML files"
  homepage "https://github.com/actions/languageservices/tree/main/languageserver"
  url "https://github.com/actions/languageservices/archive/refs/tags/release-v0.3.62.tar.gz"
  sha256 "b6d469b5af6e87ff55e125b203081ff026204f7f670d03d8c0c7820c40768961"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "6642e7d36c67fee1cf5b86a748191ed7947d5a05931d30257cb2ccb917d5c48d"
  end

  depends_on "node"

  deny_network_access!

  def fetch
    system "npm", "install", *std_npm_args(prefix: false)
  end

  def install
    system "npm", "run", "build", "--workspaces"
    libexec.install "languageserver/bin", "languageserver/dist"
    bin.install_symlink Dir[libexec/"bin/*"]
  end

  test do
    require "open3"

    request = {
      jsonrpc: "2.0",
      id:      1,
      method:  "initialize",
      params:  { rootUri: nil, capabilities: {} },
    }.to_json

    Open3.popen3(bin/"actions-languageserver", "--stdio") do |stdin, stdout|
      stdin.write "Content-Length: #{request.bytesize}\r\n\r\n#{request}"
      sleep 3
      assert_match(/^Content-Length: \d+/i, stdout.readline)
    end
  end
end
