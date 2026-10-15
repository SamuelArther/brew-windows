class Unixodbc < Formula
  desc "ODBC 3 connectivity for UNIX"
  homepage "https://www.unixodbc.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-unixodbc-2.3.14-1-any.pkg.tar.zst"
  sha256 "fd1bd7fa7b061c9174d2246d4f37e1ac21eedaaebceaeac7b23a95512c695c79"
  license all_of: [
    "LGPL-2.1-or-later", # libraries
    "GPL-2.0-or-later",  # programs
  ]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://www.unixodbc.org/download.html"
    regex(/href=.*?unixODBC[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libiconv" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libiconv-1.19-1-any.pkg.tar.zst"
    sha256 "9a500f38c2b91808741c62fae746b3e9110b33a1ecf5c30fa0c66dbedddf7e16"
  end

  resource "mingw-w64-ucrt-x86_64-libltdl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libltdl-2.6.2-1-any.pkg.tar.zst"
    sha256 "d769ff3399df0c83010ebc16696f0265e3f078469a5087719b235f24c8724a9e"
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
    msys2_bin "ucrt64/bin/dltest.exe", "dltest"
    msys2_bin "ucrt64/bin/isql.exe", "isql"
    msys2_bin "ucrt64/bin/iusql.exe", "iusql"
    msys2_bin "ucrt64/bin/odbc_config.exe", "odbc_config"
    msys2_bin "ucrt64/bin/odbcinst.exe", "odbcinst"
    msys2_bin "ucrt64/bin/slencheck.exe", "slencheck"
  end

  test do
    system bin/"odbcinst", "-j"
  end
end
