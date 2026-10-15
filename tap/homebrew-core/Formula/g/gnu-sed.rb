class GnuSed < Formula
  desc "GNU implementation of the famous stream editor"
  homepage "https://www.gnu.org/software/sed/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-sed-4.9-1-any.pkg.tar.zst"
  sha256 "f16b0145ffd7c53d1249c093bed3b2a52f43609440952db833bfe64ead79e7ea"
  license "GPL-3.0-or-later"

  # Native UCRT64 GNU sed, exposed as sed and gsed.

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-gettext-libtextstyle" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gettext-libtextstyle-1.0-1-any.pkg.tar.zst"
    sha256 "a9a9254c8f1b0bcedfe83573b14c538df4bf0eb0e6448888ca0f785dfc9cf016"
  end

  resource "mingw-w64-ucrt-x86_64-gettext-runtime" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gettext-runtime-1.0-1-any.pkg.tar.zst"
    sha256 "ba693dda4ac375af76ce481ff3a6e7481286546cc7dc6d56c7021dae34084157"
  end

  resource "mingw-w64-ucrt-x86_64-gettext-tools" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gettext-tools-1.0-1-any.pkg.tar.zst"
    sha256 "e2b017e98dfed1ff026b6d5fc26b228d05d8a0ffc00077727018109f694eee5b"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libiconv" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libiconv-1.19-1-any.pkg.tar.zst"
    sha256 "9a500f38c2b91808741c62fae746b3e9110b33a1ecf5c30fa0c66dbedddf7e16"
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
    msys2_bin "ucrt64/bin/sed.exe", "sed"
    msys2_bin "ucrt64/bin/sed.exe", "gsed"
  end

  test do
    (testpath/"test.txt").write "Hello world!"
    system bin/"sed", "-i", "s/world/World/g", "test.txt"
    assert_match "Hello World!", File.read("test.txt")
  end
end
