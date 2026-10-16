class Zls < Formula
  desc "Language Server for Zig"
  homepage "https://zigtools.org/zls/"
  url "https://github.com/zigtools/zls/releases/download/0.16.0/zls-x86_64-windows.zip"
  sha256 "35cbb7163224e8cf92d21099c1b1391f2aba927f25d389f021b13a21d40b96dd"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/zigtools/zls/archive/refs/tags/0.16.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/zls.exe", "zls"
  end

  test do
    test_config = testpath/"zls.json"
    test_config.write <<~JSON
      {
        "enable_semantic_tokens": true
      }
    JSON

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
    output = pipe_output("#{bin}/zls --config-path #{test_config}", input, 1)
    assert_match(/^Content-Length: \d+/i, output)

    assert_match version.to_s, shell_output("#{bin}/zls --version")
  end
end
