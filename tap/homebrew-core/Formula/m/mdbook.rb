class Mdbook < Formula
  desc "Create modern online books from Markdown files"
  homepage "https://rust-lang.github.io/mdBook/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-mdbook-0.5.4-1-any.pkg.tar.zst"
  sha256 "01c314a013e241b19aeae5c1b49edab1e231aaf003a4e5b3304bfbbe94a9b97a"
  license "MPL-2.0"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/rust-lang/mdBook/archive/refs/tags/v0.5.4.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/mdbook.exe", "mdbook"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mdbook --version 2>&1")
  end
end
