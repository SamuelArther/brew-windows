class Pngnq < Formula
  desc "Tool for optimizing PNG images"
  homepage "https://pngnq.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-pngnq-1.1-5-any.pkg.tar.zst"
  sha256 "4ac6ffa613f8c3164278f64c885058f72f14ad5e13862ff6f85a39bda82a5125"
  license "BSD-3-Clause"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://downloads.sourceforge.net/project/pngnq/pngnq/1.1/pngnq-1.1.tar.gz"
  end

  resource "mingw-w64-ucrt-x86_64-libpng" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libpng-1.6.59-1-any.pkg.tar.zst"
    sha256 "7f8448e06eceba8f8ce130390addea49f767bdf8f6b7e7eede8ca6d595e3d3a8"
  end

  resource "mingw-w64-ucrt-x86_64-zlib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-1.3.2-2-any.pkg.tar.zst"
    sha256 "841401182976d2f9e17e5c0ebaac51f2a8014140ea53d67625e91c8fb3c85ea0"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/pngcomp.exe", "pngcomp"
    msys2_bin "ucrt64/bin/pngnq.exe", "pngnq"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/pngnq -V 2>&1")
  end
end
