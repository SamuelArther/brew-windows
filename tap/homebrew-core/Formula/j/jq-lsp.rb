class JqLsp < Formula
  desc "Jq language server"
  homepage "https://github.com/wader/jq-lsp"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-jq-lsp-0.1.15-1-any.pkg.tar.zst"
  sha256 "35860eea5e33d0b114065faf5cc76e1f36cac5f53806c559112757e9dc13e6e3"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/wader/jq-lsp/archive/refs/tags/v0.1.19.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/jq-lsp.exe", "jq-lsp"
  end

  test do
    system bin/"jq-lsp", "--version"
  end
end
