class GolangciLintLangserver < Formula
  desc "Language server for `golangci-lint`"
  homepage "https://github.com/nametake/golangci-lint-langserver"
  url "https://github.com/nametake/golangci-lint-langserver/releases/download/v0.0.12/golangci-lint-langserver_Windows_x86_64.zip"
  sha256 "39b900b6b18bdc0328a9a58539af2bfc0bdaad9c2f217c1afa7921717d11e4d6"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/nametake/golangci-lint-langserver/archive/refs/tags/v0.0.12.tar.gz"
  end

  def install
    bin.install "golangci-lint-langserver.exe"
  end

  test do
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

    input = "Content-Length: #{json.size}\r\n\r\n#{json}"
    output = pipe_output(bin/"golangci-lint-langserver", input)
    assert_match(/^Content-Length: \d+/i, output)
  end
end
