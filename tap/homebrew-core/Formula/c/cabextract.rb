class Cabextract < Formula
  desc "Extract files from Microsoft cabinet files"
  homepage "https://www.cabextract.org.uk/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cabextract-1.11-1-any.pkg.tar.zst"
  sha256 "d234c38b2834b6be6cdadfda525dbc4a736674814860d8e1d8824fc3f9314236"
  license "GPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url :homepage
    regex(/href=.*?cabextract[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  resource "mingw-w64-ucrt-x86_64-libiconv" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libiconv-1.19-1-any.pkg.tar.zst"
    sha256 "9a500f38c2b91808741c62fae746b3e9110b33a1ecf5c30fa0c66dbedddf7e16"
  end

  resource "mingw-w64-ucrt-x86_64-libmspack" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libmspack-1.11-1-any.pkg.tar.zst"
    sha256 "2376f041a443d075647572574f709c7d753e35123f2c2b15e0be2ca00efaf8ef"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/cabextract.exe", "cabextract"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cabextract --version 2>&1")
  end
end
