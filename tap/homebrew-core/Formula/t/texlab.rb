class Texlab < Formula
  desc "Implementation of the Language Server Protocol for LaTeX"
  homepage "https://github.com/latex-lsp/texlab/"
  url "https://github.com/latex-lsp/texlab/releases/download/v5.26.0/texlab-x86_64-windows.zip"
  sha256 "cb028d44c3d2b85d36a2ed52d41a0ff43a341b1f04c500c56c4524c4eb72b316"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/latex-lsp/texlab/archive/refs/tags/v5.26.0.tar.gz"
  end

  def install
    bin.install "texlab.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/texlab --version 2>&1")
  end
end
