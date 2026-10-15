class UutilsFindutils < Formula
  desc "Cross-platform Rust rewrite of the GNU findutils"
  homepage "https://uutils.github.io/findutils/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-uutils-findutils-0.10.0-2-any.pkg.tar.zst"
  sha256 "5e3ce31e8083dc9fb98a3586dc5088e55ee67ebfefc7412a0869b76ab92e7f59"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/uutils/findutils/archive/refs/tags/0.10.0.tar.gz"
  end

  resource "mingw-w64-ucrt-x86_64-oniguruma" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-oniguruma-6.9.10-1-any.pkg.tar.zst"
    sha256 "e1132938ebc40f8d820fae202c3de1493be3a606bd454c9e1c619ed730dd5697"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/uu-find.exe", "uu-find"
    msys2_bin "ucrt64/bin/uu-locate.exe", "uu-locate"
    msys2_bin "ucrt64/bin/uu-updatedb.exe", "uu-updatedb"
    msys2_bin "ucrt64/bin/uu-xargs.exe", "uu-xargs"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/uu-find --version 2>&1")
  end
end
