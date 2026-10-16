class TreeSitterCli < Formula
  desc "Parser generator tool"
  homepage "https://tree-sitter.github.io"
  url "https://github.com/tree-sitter/tree-sitter/releases/download/v0.27.1/tree-sitter-cli-windows-x64.zip"
  sha256 "de043e4c289efcef7981df99988300f23763befdeb36abf7c1898f080c478424"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/tree-sitter/tree-sitter/archive/refs/tags/v0.27.1.tar.gz"
  end

  def install
    bin.install "tree-sitter.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tree-sitter --version 2>&1")
  end
end
