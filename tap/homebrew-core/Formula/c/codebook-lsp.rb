class CodebookLsp < Formula
  desc "Code-aware spell checker language server"
  homepage "https://github.com/blopker/codebook"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-codebook-lsp-0.3.42-1-any.pkg.tar.zst"
  sha256 "63fb79a20254c362c3a91c0d370270b2eee99a00d50bcbd4344ca4e3b17b0b7e"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/blopker/codebook/archive/refs/tags/v0.3.42.tar.gz"
    strategy :github_latest
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/codebook-lsp.exe", "codebook-lsp"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/codebook-lsp --version 2>&1")
  end
end
