class JustLsp < Formula
  desc "Language server for just"
  homepage "https://github.com/terror/just-lsp"
  url "https://github.com/terror/just-lsp/releases/download/0.10.0/just-lsp-0.10.0-x86_64-pc-windows-msvc.zip"
  sha256 "4465270a43bc71e9ed917320b780a80b53ce632fac83aeafadb689ab8f056f1d"
  license "CC0-1.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/terror/just-lsp/archive/refs/tags/0.10.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/just-lsp.exe", "just-lsp"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/just-lsp --version 2>&1")
  end
end
