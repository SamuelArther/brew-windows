class Libirecovery < Formula
  desc "Library and utility to talk to iBoot/iBSS via USB"
  homepage "https://www.libimobiledevice.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libirecovery-1.2.1-1-any.pkg.tar.zst"
  sha256 "9b1af519db640f4bb9bb25cf947d1c18f413702ca1558e5df463df04ed8e12d7"
  license "LGPL-2.1-only"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/libimobiledevice/libirecovery/releases/download/1.3.1/libirecovery-1.3.1.tar.bz2"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libimobiledevice-glue" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libimobiledevice-glue-1.3.2-1-any.pkg.tar.zst"
    sha256 "d089c718ab846c5bd1f41fb2788a976fdbda13c9f09128a670eca578da0bc34f"
  end

  resource "mingw-w64-ucrt-x86_64-libplist" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libplist-2.7.0-4-any.pkg.tar.zst"
    sha256 "665607c34dbf923ebb820d78b376db59de4d2d3874a45d70f24f51eb1d64f127"
  end

  resource "mingw-w64-ucrt-x86_64-libstdc++" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libstdc++-16.2.0-6-any.pkg.tar.zst"
    sha256 "33c5e58a85a538cf843a8672991cce591f0231d66da3b66a57629712d64be17f"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-readline" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-readline-8.3.003-1-any.pkg.tar.zst"
    sha256 "de2423c2e10fcd88272a0ab2f833f6a082cfe613d4c17c2f548cc50a5d2190c4"
  end

  resource "mingw-w64-ucrt-x86_64-termcap" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-termcap-1.3.1-8-any.pkg.tar.zst"
    sha256 "c7891143e252568d8d46df72b3aefea932f4166771e17f22317218d7dd2af23c"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/irecovery.exe", "irecovery"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/irecovery --version 2>&1")
  end
end
