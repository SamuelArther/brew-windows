class Libgig < Formula
  desc "Library for Gigasampler and DLS (Downloadable Sounds) Level 1/2 files"
  homepage "https://www.linuxsampler.org/libgig/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgig-4.5.2-1-any.pkg.tar.zst"
  sha256 "0dd1a38507e41195102d4dba7ad2262e6efca3ca7c0c0df5d6b497e9bab8e459"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "http://download.linuxsampler.org/packages/"
    regex(/href=.*?libgig[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-flac" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-flac-1.5.0-2-any.pkg.tar.zst"
    sha256 "28e8637795bd7d80289ddbb582d6ad350b73eac64390b877149717dd3757ad79"
  end

  resource "mingw-w64-ucrt-x86_64-lame" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lame-3.100-3-any.pkg.tar.zst"
    sha256 "6bc4bf7d2f39a941a695b228818a8c846a53c43c20117e0707e08977b0dd0da9"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libiconv" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libiconv-1.19-1-any.pkg.tar.zst"
    sha256 "9a500f38c2b91808741c62fae746b3e9110b33a1ecf5c30fa0c66dbedddf7e16"
  end

  resource "mingw-w64-ucrt-x86_64-libogg" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libogg-1.3.6-1-any.pkg.tar.zst"
    sha256 "110a34239e8f122da15e041b2601e2f3c6d4c909d738bdf978700601e0f64449"
  end

  resource "mingw-w64-ucrt-x86_64-libsndfile" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libsndfile-1.2.2-1-any.pkg.tar.zst"
    sha256 "a882301af8cb1f645c45b1dcbf9634a9bf5a92af47f0cef95a88e83d3a3f444b"
  end

  resource "mingw-w64-ucrt-x86_64-libstdc++" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libstdc++-16.2.0-6-any.pkg.tar.zst"
    sha256 "33c5e58a85a538cf843a8672991cce591f0231d66da3b66a57629712d64be17f"
  end

  resource "mingw-w64-ucrt-x86_64-libvorbis" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libvorbis-1.3.7-3-any.pkg.tar.zst"
    sha256 "d013d2c7489b175299f9ea8bd9b70cf456337943b0f13e6eb470beb6c3fc251f"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-mpg123" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-mpg123-1.33.7-1-any.pkg.tar.zst"
    sha256 "7d4efa8ab7682476f0b8b048658596e9b64d8115cd9706afbba0a5410792e240"
  end

  resource "mingw-w64-ucrt-x86_64-opus" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-opus-1.6.1-1-any.pkg.tar.zst"
    sha256 "7b8f176f9cc0431b52006861c0a2e083d73d4b47a73ab749755b5379ff673252"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/akaidump.exe", "akaidump"
    msys2_bin "ucrt64/bin/akaiextract.exe", "akaiextract"
    msys2_bin "ucrt64/bin/dlsdump.exe", "dlsdump"
    msys2_bin "ucrt64/bin/gig2mono.exe", "gig2mono"
    msys2_bin "ucrt64/bin/gig2stereo.exe", "gig2stereo"
    msys2_bin "ucrt64/bin/gigdump.exe", "gigdump"
    msys2_bin "ucrt64/bin/gigextract.exe", "gigextract"
    msys2_bin "ucrt64/bin/gigmerge.exe", "gigmerge"
    msys2_bin "ucrt64/bin/korg2gig.exe", "korg2gig"
    msys2_bin "ucrt64/bin/korgdump.exe", "korgdump"
    msys2_bin "ucrt64/bin/rifftree.exe", "rifftree"
    msys2_bin "ucrt64/bin/sf2dump.exe", "sf2dump"
    msys2_bin "ucrt64/bin/sf2extract.exe", "sf2extract"
    msys2_bin "ucrt64/bin/wav2gig.exe", "wav2gig"
  end

  test do
    assert_path_exists bin/"akaidump.exe"
  end
end
