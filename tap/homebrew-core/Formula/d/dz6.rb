class Dz6 < Formula
  desc "Fast Vim-inspired TUI hex editor"
  homepage "https://dz6.dev.br"
  url "https://github.com/mentebinaria/dz6/releases/download/v0.8.0/dz6-x86_64-pc-windows-msvc.zip"
  sha256 "4c5d3d2d41a3c4c60cf6a7d04cc7362a359096f68366973885e7f458ccd59bd4"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mentebinaria/dz6/archive/refs/tags/v0.8.0.tar.gz"
  end

  def install
    bin.install "dz6.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dz6 --version 2>&1")
  end
end
