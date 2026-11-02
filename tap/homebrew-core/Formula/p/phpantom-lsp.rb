class PhpantomLsp < Formula
  desc "Fast PHP language server written in Rust"
  homepage "https://github.com/PHPantom-dev/phpantom_lsp"
  url "https://github.com/PHPantom-dev/phpantom_lsp/archive/refs/tags/0.11.1.tar.gz"
  sha256 "c5a9b0e21eac77edacd56add5b46094fb2425435581f32ba574d25e1ff6b97df"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "7365821ba712713e48dee32bda2891019d48d204990b94f1e0a0fc4d77dd904b"
  end

  depends_on "rust" => :build

  # The PHP standard library stubs embedded into the binary, pinned in `stubs.lock`.
  # build.rs downloads them when they are missing, which the sandbox blocks.
  resource "phpstorm-stubs" do
    url "https://github.com/JetBrains/phpstorm-stubs/archive/5f68c1021badebe34119fb8fa10ba7cc25de6c0c.tar.gz"
    version "5f68c1021badebe34119fb8fa10ba7cc25de6c0c"
    sha256 "8b9bf4b69196d9193f56a4a6ad88b9ff8a230e372312ae8a181c04df1972c4a1"

    livecheck do
      url "https://raw.githubusercontent.com/PHPantom-dev/phpantom_lsp/refs/tags/#{LATEST_VERSION}/stubs.lock"
      regex(/^commit\s*=\s*["'](\h+)["']/i)
    end
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    stubs = buildpath/"stubs/jetbrains/phpstorm-stubs"
    resource("phpstorm-stubs").stage stubs
    # build.rs treats stubs without a matching commit marker as stale and re-downloads them.
    (stubs/".commit").write resource("phpstorm-stubs").version

    system "cargo", "install", *std_cargo_args
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
    output = pipe_output("#{bin}/phpantom_lsp --stdio 2>&1", input, 0)
    assert_match(/^Content-Length: \d+/i, output)

    # Built-in functions only resolve when the stubs were embedded.
    (testpath/"test.php").write <<~PHP
      <?php
      echo in_array(1, [1, 2]) ? count([1]) : strlen("x");
    PHP
    assert_match "No errors", shell_output("#{bin}/phpantom_lsp analyze --project-root #{testpath} --no-colour")
  end
end
