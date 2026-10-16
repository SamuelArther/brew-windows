class Libvisio < Formula
  desc "Interpret and import Visio diagrams"
  homepage "https://wiki.documentfoundation.org/DLP/Libraries/libvisio"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libvisio-0.1.11-2-any.pkg.tar.zst"
  sha256 "0a67c772ac9b46c03fa1484947782edd6f8f6d1d4ee511a37ebe16a448bb00d3"
  license "MPL-2.0"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://dev-www.libreoffice.org/src/"
    regex(/href=["']?libvisio[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-icu" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-icu-78.3-4-any.pkg.tar.zst"
    sha256 "db9282bbd0a2e74c31d66af6bd89eb68d647a09f721eef4c661768588dca4324"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libiconv" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libiconv-1.19-1-any.pkg.tar.zst"
    sha256 "9a500f38c2b91808741c62fae746b3e9110b33a1ecf5c30fa0c66dbedddf7e16"
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

  resource "mingw-w64-ucrt-x86_64-libxml2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libxml2-2.15.4-1-any.pkg.tar.zst"
    sha256 "ea6b8e7ee24d8674e719f1f0a1b3747a72158ab786dfc5392ccb9f8edd09582a"
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
    msys2_bin "ucrt64/bin/vsd2raw.exe", "vsd2raw"
    msys2_bin "ucrt64/bin/vsd2text.exe", "vsd2text"
    msys2_bin "ucrt64/bin/vsd2xhtml.exe", "vsd2xhtml"
    msys2_bin "ucrt64/bin/vss2raw.exe", "vss2raw"
    msys2_bin "ucrt64/bin/vss2text.exe", "vss2text"
    msys2_bin "ucrt64/bin/vss2xhtml.exe", "vss2xhtml"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/vsd2raw --version 2>&1")
  end
end
