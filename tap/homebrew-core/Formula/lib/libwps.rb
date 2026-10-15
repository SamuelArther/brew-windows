class Libwps < Formula
  desc "Library to import files in MS Works format"
  homepage "https://libwps.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwps-0.4.14-2-any.pkg.tar.zst"
  sha256 "c351f3f47c97726720b5288a45edcce2ae307a14fc7623bd0a320df61d93073a"
  license any_of: ["LGPL-2.1-or-later", "MPL-2.0"]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://downloads.sourceforge.net/project/libwps/libwps/libwps-0.4.14/libwps-0.4.14.tar.xz"
    regex(%r{url=.*?/libwps(?:/|[._-])v?(\d+(?:\.\d+)+)\.t}i)
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-librevenge" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-librevenge-0.0.6-1-any.pkg.tar.zst"
    sha256 "c614473fdacab65f941ed23ea29c5f13582dfc2355f07d3cf6079258589b192f"
  end

  resource "mingw-w64-ucrt-x86_64-libstdc++" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libstdc++-16.2.0-6-any.pkg.tar.zst"
    sha256 "33c5e58a85a538cf843a8672991cce591f0231d66da3b66a57629712d64be17f"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  resource "mingw-w64-ucrt-x86_64-zlib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-1.3.2-2-any.pkg.tar.zst"
    sha256 "841401182976d2f9e17e5c0ebaac51f2a8014140ea53d67625e91c8fb3c85ea0"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/wks2csv.exe", "wks2csv"
    msys2_bin "ucrt64/bin/wks2raw.exe", "wks2raw"
    msys2_bin "ucrt64/bin/wks2text.exe", "wks2text"
    msys2_bin "ucrt64/bin/wps2html.exe", "wps2html"
    msys2_bin "ucrt64/bin/wps2raw.exe", "wps2raw"
    msys2_bin "ucrt64/bin/wps2text.exe", "wps2text"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/wks2csv --version 2>&1")
  end
end
