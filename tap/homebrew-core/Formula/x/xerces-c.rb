class XercesC < Formula
  desc "Validating XML parser"
  homepage "https://xerces.apache.org/xerces-c/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-xerces-c-3.3.0-4-any.pkg.tar.zst"
  sha256 "19a3ddf5bccde4078478b5a37bbe5ffd9d13cc1a2c3192292d8919b2703e62a0"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://www.apache.org/dyn/closer.lua?path=xerces/c/3/sources/xerces-c-3.3.0.tar.gz"
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

  def install
    msys2_install
    msys2_bin "ucrt64/bin/CreateDOMDocument.exe", "CreateDOMDocument"
    msys2_bin "ucrt64/bin/DOMCount.exe", "DOMCount"
    msys2_bin "ucrt64/bin/DOMPrint.exe", "DOMPrint"
    msys2_bin "ucrt64/bin/EnumVal.exe", "EnumVal"
    msys2_bin "ucrt64/bin/MemParse.exe", "MemParse"
    msys2_bin "ucrt64/bin/PParse.exe", "PParse"
    msys2_bin "ucrt64/bin/PSVIWriter.exe", "PSVIWriter"
    msys2_bin "ucrt64/bin/Redirect.exe", "Redirect"
    msys2_bin "ucrt64/bin/SAX2Count.exe", "SAX2Count"
    msys2_bin "ucrt64/bin/SAX2Print.exe", "SAX2Print"
    msys2_bin "ucrt64/bin/SAXCount.exe", "SAXCount"
    msys2_bin "ucrt64/bin/SAXPrint.exe", "SAXPrint"
    msys2_bin "ucrt64/bin/SCMPrint.exe", "SCMPrint"
    msys2_bin "ucrt64/bin/SEnumVal.exe", "SEnumVal"
    msys2_bin "ucrt64/bin/StdInParse.exe", "StdInParse"
    msys2_bin "ucrt64/bin/XInclude.exe", "XInclude"
  end

  test do
    (testpath/"ducks.xml").write <<~XML
      <?xml version="1.0" encoding="iso-8859-1"?>

      <ducks>
        <person id="Red.Duck" >
          <name><family>Duck</family> <given>One</given></name>
          <email>duck@foo.com</email>
        </person>
      </ducks>
    XML

    output = shell_output("#{bin}/SAXCount #{testpath}/ducks.xml")
    assert_match "(6 elems, 1 attrs, 0 spaces, 37 chars)", output
  end
end
