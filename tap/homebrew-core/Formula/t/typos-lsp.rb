class TyposLsp < Formula
  desc "Language Server for typos-cli"
  homepage "https://github.com/tekumara/typos-lsp"
  url "https://github.com/tekumara/typos-lsp/releases/download/v0.1.57/typos-lsp-v0.1.57-x86_64-pc-windows-msvc.zip"
  sha256 "b92bc70370de95a908668563acfff019ab7fcf4a151998576eb2d2e229180fcb"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/tekumara/typos-lsp/archive/refs/tags/v0.1.57.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/x86_64-pc-windows-msvc/release/typos-lsp.exe", "typos-lsp"
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
    output = pipe_output(bin/"typos-lsp", input)
    assert_match(/^Content-Length: \d+/i, output)
  end
end
