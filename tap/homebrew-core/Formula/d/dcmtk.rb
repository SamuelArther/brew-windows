class Dcmtk < Formula
  desc "OFFIS DICOM toolkit command-line utilities"
  homepage "https://dcmtk.org/en/dcmtk/", browsed: "2026-08-06"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-dcmtk-3.7.0-4-any.pkg.tar.zst"
  sha256 "7b3a20012e02408a2af0cccfd4a04d05ace5b85cef1a2f1bb45941147751c6cb"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url :head
    regex(/^dcmtk[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-flac" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-flac-1.5.0-2-any.pkg.tar.zst"
    sha256 "28e8637795bd7d80289ddbb582d6ad350b73eac64390b877149717dd3757ad79"
  end

  resource "mingw-w64-ucrt-x86_64-gettext-runtime" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gettext-runtime-1.0-1-any.pkg.tar.zst"
    sha256 "ba693dda4ac375af76ce481ff3a6e7481286546cc7dc6d56c7021dae34084157"
  end

  resource "mingw-w64-ucrt-x86_64-giflib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-giflib-6.1.3-1-any.pkg.tar.zst"
    sha256 "9e7ac8feb0e2af6e0fce3c8ead1bd7bcaf018661d08033858c1e2657a04f6f1a"
  end

  resource "mingw-w64-ucrt-x86_64-jbigkit" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-jbigkit-2.1-6-any.pkg.tar.zst"
    sha256 "9e237d1041350e7436294cfa13d4bd3d1606d1f2a32ade2eb2958b4d4e79b7ad"
  end

  resource "mingw-w64-ucrt-x86_64-lame" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lame-3.100-3-any.pkg.tar.zst"
    sha256 "6bc4bf7d2f39a941a695b228818a8c846a53c43c20117e0707e08977b0dd0da9"
  end

  resource "mingw-w64-ucrt-x86_64-lcms2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lcms2-2.19.1-1-any.pkg.tar.zst"
    sha256 "144518cc80679f7bcc1d31b5dd7ec92e8737a1d8162014e30c8c1c181f47a156"
  end

  resource "mingw-w64-ucrt-x86_64-lerc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lerc-4.1.1-1-any.pkg.tar.zst"
    sha256 "c0afbf4832488d67030c6f313349cd814946c9a7bc3ae9fd54524825c9ca055d"
  end

  resource "mingw-w64-ucrt-x86_64-libdeflate" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libdeflate-1.26-1-any.pkg.tar.zst"
    sha256 "17720c77bd01641a3554801cafd71d96fb7fff7427f4cc20ac9f36efb8cb3da0"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libiconv" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libiconv-1.19-1-any.pkg.tar.zst"
    sha256 "9a500f38c2b91808741c62fae746b3e9110b33a1ecf5c30fa0c66dbedddf7e16"
  end

  resource "mingw-w64-ucrt-x86_64-libjpeg-turbo" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libjpeg-turbo-3.2.0-1-any.pkg.tar.zst"
    sha256 "7c001d00df557cfee969ad43d37b9c31ae22869e8aac4091a89dc3ae6d95e11d"
  end

  resource "mingw-w64-ucrt-x86_64-libogg" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libogg-1.3.6-1-any.pkg.tar.zst"
    sha256 "110a34239e8f122da15e041b2601e2f3c6d4c909d738bdf978700601e0f64449"
  end

  resource "mingw-w64-ucrt-x86_64-libpng" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libpng-1.6.59-1-any.pkg.tar.zst"
    sha256 "7f8448e06eceba8f8ce130390addea49f767bdf8f6b7e7eede8ca6d595e3d3a8"
  end

  resource "mingw-w64-ucrt-x86_64-libsndfile" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libsndfile-1.2.2-1-any.pkg.tar.zst"
    sha256 "a882301af8cb1f645c45b1dcbf9634a9bf5a92af47f0cef95a88e83d3a3f444b"
  end

  resource "mingw-w64-ucrt-x86_64-libstdc++" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libstdc++-16.2.0-6-any.pkg.tar.zst"
    sha256 "33c5e58a85a538cf843a8672991cce591f0231d66da3b66a57629712d64be17f"
  end

  resource "mingw-w64-ucrt-x86_64-libtiff" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libtiff-4.7.2-1-any.pkg.tar.zst"
    sha256 "76904b28133fc3cc0a0e518846c98d3d2166fac70ed9c6184dfe5ef145e5f279"
  end

  resource "mingw-w64-ucrt-x86_64-libvorbis" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libvorbis-1.3.7-3-any.pkg.tar.zst"
    sha256 "d013d2c7489b175299f9ea8bd9b70cf456337943b0f13e6eb470beb6c3fc251f"
  end

  resource "mingw-w64-ucrt-x86_64-libwebp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwebp-1.6.0-1-any.pkg.tar.zst"
    sha256 "34ff8396c403d484c8777ad49ad0fc06dfb94fadf6f14bcf84d50c45bc6fd17e"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-libxml2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libxml2-2.15.4-1-any.pkg.tar.zst"
    sha256 "ea6b8e7ee24d8674e719f1f0a1b3747a72158ab786dfc5392ccb9f8edd09582a"
  end

  resource "mingw-w64-ucrt-x86_64-mpg123" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-mpg123-1.33.7-1-any.pkg.tar.zst"
    sha256 "7d4efa8ab7682476f0b8b048658596e9b64d8115cd9706afbba0a5410792e240"
  end

  resource "mingw-w64-ucrt-x86_64-openjpeg2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openjpeg2-2.5.4-2-any.pkg.tar.zst"
    sha256 "2a4c148052b6f8e47abe5e98b29fd8942e2e3c17722ff00f172b1630897a10b9"
  end

  resource "mingw-w64-ucrt-x86_64-openssl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openssl-3.6.5-1-any.pkg.tar.zst"
    sha256 "773e021ebad83f14b5c103db467411e8ff064663e1c7e7f730e2be72f4117064"
  end

  resource "mingw-w64-ucrt-x86_64-opus" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-opus-1.6.1-1-any.pkg.tar.zst"
    sha256 "7b8f176f9cc0431b52006861c0a2e083d73d4b47a73ab749755b5379ff673252"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  resource "mingw-w64-ucrt-x86_64-xz" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-xz-5.8.4-1-any.pkg.tar.zst"
    sha256 "a9d771f816d8c0c5bde152ffb27a687ede91079b8e77e5b8297efa3b5a835934"
  end

  resource "mingw-w64-ucrt-x86_64-zlib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-1.3.2-2-any.pkg.tar.zst"
    sha256 "841401182976d2f9e17e5c0ebaac51f2a8014140ea53d67625e91c8fb3c85ea0"
  end

  resource "mingw-w64-ucrt-x86_64-zstd" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zstd-1.5.7-2-any.pkg.tar.zst"
    sha256 "dbdb8427280046a2b41697780aa4c52983b708082b0da4755951dc3bea96ca89"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/cda2dcm.exe", "cda2dcm"
    msys2_bin "ucrt64/bin/dcm2cda.exe", "dcm2cda"
    msys2_bin "ucrt64/bin/dcm2img.exe", "dcm2img"
    msys2_bin "ucrt64/bin/dcm2json.exe", "dcm2json"
    msys2_bin "ucrt64/bin/dcm2pdf.exe", "dcm2pdf"
    msys2_bin "ucrt64/bin/dcm2pnm.exe", "dcm2pnm"
    msys2_bin "ucrt64/bin/dcm2xml.exe", "dcm2xml"
    msys2_bin "ucrt64/bin/dcmcjpeg.exe", "dcmcjpeg"
    msys2_bin "ucrt64/bin/dcmcjpls.exe", "dcmcjpls"
    msys2_bin "ucrt64/bin/dcmconv.exe", "dcmconv"
    msys2_bin "ucrt64/bin/dcmcrle.exe", "dcmcrle"
    msys2_bin "ucrt64/bin/dcmdecap.exe", "dcmdecap"
    msys2_bin "ucrt64/bin/dcmdjpeg.exe", "dcmdjpeg"
    msys2_bin "ucrt64/bin/dcmdjpls.exe", "dcmdjpls"
    msys2_bin "ucrt64/bin/dcmdrle.exe", "dcmdrle"
    msys2_bin "ucrt64/bin/dcmdspfn.exe", "dcmdspfn"
    msys2_bin "ucrt64/bin/dcmdump.exe", "dcmdump"
    msys2_bin "ucrt64/bin/dcmencap.exe", "dcmencap"
    msys2_bin "ucrt64/bin/dcmftest.exe", "dcmftest"
    msys2_bin "ucrt64/bin/dcmgpdir.exe", "dcmgpdir"
    msys2_bin "ucrt64/bin/dcmicmp.exe", "dcmicmp"
    msys2_bin "ucrt64/bin/dcmj2pnm.exe", "dcmj2pnm"
    msys2_bin "ucrt64/bin/dcml2pnm.exe", "dcml2pnm"
    msys2_bin "ucrt64/bin/dcmmkcrv.exe", "dcmmkcrv"
    msys2_bin "ucrt64/bin/dcmmkdir.exe", "dcmmkdir"
    msys2_bin "ucrt64/bin/dcmmklut.exe", "dcmmklut"
    msys2_bin "ucrt64/bin/dcmodify.exe", "dcmodify"
    msys2_bin "ucrt64/bin/dcmp2pgm.exe", "dcmp2pgm"
    msys2_bin "ucrt64/bin/dcmprscp.exe", "dcmprscp"
    msys2_bin "ucrt64/bin/dcmprscu.exe", "dcmprscu"
    msys2_bin "ucrt64/bin/dcmpschk.exe", "dcmpschk"
    msys2_bin "ucrt64/bin/dcmpsmk.exe", "dcmpsmk"
    msys2_bin "ucrt64/bin/dcmpsprt.exe", "dcmpsprt"
    msys2_bin "ucrt64/bin/dcmpsrcv.exe", "dcmpsrcv"
    msys2_bin "ucrt64/bin/dcmpssnd.exe", "dcmpssnd"
    msys2_bin "ucrt64/bin/dcmqridx.exe", "dcmqridx"
    msys2_bin "ucrt64/bin/dcmqrscp.exe", "dcmqrscp"
    msys2_bin "ucrt64/bin/dcmqrti.exe", "dcmqrti"
    msys2_bin "ucrt64/bin/dcmquant.exe", "dcmquant"
    msys2_bin "ucrt64/bin/dcmrecv.exe", "dcmrecv"
    msys2_bin "ucrt64/bin/dcmscale.exe", "dcmscale"
    msys2_bin "ucrt64/bin/dcmsend.exe", "dcmsend"
    msys2_bin "ucrt64/bin/dcmsign.exe", "dcmsign"
    msys2_bin "ucrt64/bin/dcod2lum.exe", "dcod2lum"
    msys2_bin "ucrt64/bin/dconvlum.exe", "dconvlum"
    msys2_bin "ucrt64/bin/drtdump.exe", "drtdump"
    msys2_bin "ucrt64/bin/dsr2html.exe", "dsr2html"
    msys2_bin "ucrt64/bin/dsr2xml.exe", "dsr2xml"
    msys2_bin "ucrt64/bin/dsrdump.exe", "dsrdump"
    msys2_bin "ucrt64/bin/dump2dcm.exe", "dump2dcm"
    msys2_bin "ucrt64/bin/echoscu.exe", "echoscu"
    msys2_bin "ucrt64/bin/findscu.exe", "findscu"
    msys2_bin "ucrt64/bin/getscu.exe", "getscu"
    msys2_bin "ucrt64/bin/img2dcm.exe", "img2dcm"
    msys2_bin "ucrt64/bin/json2dcm.exe", "json2dcm"
    msys2_bin "ucrt64/bin/mkcsmapper.exe", "mkcsmapper"
    msys2_bin "ucrt64/bin/mkesdb.exe", "mkesdb"
    msys2_bin "ucrt64/bin/movescu.exe", "movescu"
    msys2_bin "ucrt64/bin/pdf2dcm.exe", "pdf2dcm"
    msys2_bin "ucrt64/bin/stl2dcm.exe", "stl2dcm"
    msys2_bin "ucrt64/bin/storescp.exe", "storescp"
    msys2_bin "ucrt64/bin/storescu.exe", "storescu"
    msys2_bin "ucrt64/bin/termscu.exe", "termscu"
    msys2_bin "ucrt64/bin/wlmscpfs.exe", "wlmscpfs"
    msys2_bin "ucrt64/bin/xml2dcm.exe", "xml2dcm"
    msys2_bin "ucrt64/bin/xml2dsr.exe", "xml2dsr"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cda2dcm --version 2>&1")
  end
end
