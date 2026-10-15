class Jbig2dec < Formula
  desc "JBIG2 decoder and library (for monochrome documents)"
  homepage "https://github.com/ArtifexSoftware/jbig2dec"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-jbig2dec-0.20-1-any.pkg.tar.zst"
  sha256 "df57282cc76e53557fcc36a812d4a07e437f1c3f36f2477d871ff416518522b3"
  license "AGPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/ArtifexSoftware/jbig2dec/releases/download/0.20/jbig2dec-0.20.tar.gz"
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
    msys2_bin "ucrt64/bin/jbig2dec.exe", "jbig2dec"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/jbig2dec --version 2>&1")
  end
end
