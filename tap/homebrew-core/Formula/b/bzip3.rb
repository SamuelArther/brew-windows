class Bzip3 < Formula
  desc "Better and stronger spiritual successor to BZip2"
  homepage "https://github.com/iczelia/bzip3"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-bzip3-1.5.4-1-any.pkg.tar.zst"
  sha256 "58372f2446e2aa31ce3ffad35642fd606b8c5187e767d66734273f9323ffe682"
  license "LGPL-3.0-only"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/iczelia/bzip3/releases/download/1.5.4/bzip3-1.5.4.tar.gz"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/bzip3.exe", "bzip3"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/bzip3 --version 2>&1")
  end
end
