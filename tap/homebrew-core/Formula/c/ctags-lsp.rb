class CtagsLsp < Formula
  desc "LSP implementation using universal-ctags as backend"
  homepage "https://github.com/netmute/ctags-lsp"
  url "https://github.com/netmute/ctags-lsp/releases/download/v0.11.0/ctags-lsp_Windows_x86_64.zip"
  sha256 "e559c89ce02a3601cab21665811e2d1013b0b5f0195b5efb2673c8cef8158e11"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/netmute/ctags-lsp/archive/refs/tags/v0.11.0.tar.gz"
  end

  def install
    bin.install "ctags-lsp.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ctags-lsp --version 2>&1")
  end
end
