class Sdcc < Formula
  desc "ANSI C compiler for Intel 8051, Maxim 80DS390, and Zilog Z80"
  homepage "https://sdcc.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-sdcc-4.6.0-1-any.pkg.tar.zst"
  sha256 "089d4ce7fb91de988da0183fe9c53ffdf3451fe32d137e8ec79aa35786fa0f5c"
  license all_of: [
    "GPL-2.0-or-later", # sdcc, sdcdb, ucsim
    "GPL-3.0-or-later", # sdcpp, sdas, sdld, sdbinutils
    :public_domain,     # packihx
    "Zlib",             # makebin
  ]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://downloads.sourceforge.net/project/sdcc/sdcc/4.6.0/sdcc-src-4.6.0.tar.bz2"
    regex(%r{url=.*?/sdcc-src[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-gettext-runtime" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gettext-runtime-1.0-1-any.pkg.tar.zst"
    sha256 "ba693dda4ac375af76ce481ff3a6e7481286546cc7dc6d56c7021dae34084157"
  end

  resource "mingw-w64-ucrt-x86_64-gputils" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gputils-1.5.2-2-any.pkg.tar.zst"
    sha256 "dca381ff36410470d9b50853d938f8356337b08e7672ff92d79794f32e1f3ac3"
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

  resource "mingw-w64-ucrt-x86_64-zlib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-1.3.2-2-any.pkg.tar.zst"
    sha256 "841401182976d2f9e17e5c0ebaac51f2a8014140ea53d67625e91c8fb3c85ea0"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/makebin.exe", "makebin"
    msys2_bin "ucrt64/bin/packihx.exe", "packihx"
    msys2_bin "ucrt64/bin/s51.exe", "s51"
    msys2_bin "ucrt64/bin/sdar.exe", "sdar"
    msys2_bin "ucrt64/bin/sdas390.exe", "sdas390"
    msys2_bin "ucrt64/bin/sdas6500.exe", "sdas6500"
    msys2_bin "ucrt64/bin/sdas6808.exe", "sdas6808"
    msys2_bin "ucrt64/bin/sdas8051.exe", "sdas8051"
    msys2_bin "ucrt64/bin/sdasf8.exe", "sdasf8"
    msys2_bin "ucrt64/bin/sdasgb.exe", "sdasgb"
    msys2_bin "ucrt64/bin/sdaspdk13.exe", "sdaspdk13"
    msys2_bin "ucrt64/bin/sdaspdk14.exe", "sdaspdk14"
    msys2_bin "ucrt64/bin/sdaspdk15.exe", "sdaspdk15"
    msys2_bin "ucrt64/bin/sdasrab.exe", "sdasrab"
    msys2_bin "ucrt64/bin/sdasstm8.exe", "sdasstm8"
    msys2_bin "ucrt64/bin/sdastlcs90.exe", "sdastlcs90"
    msys2_bin "ucrt64/bin/sdasz80.exe", "sdasz80"
    msys2_bin "ucrt64/bin/sdcc.exe", "sdcc"
    msys2_bin "ucrt64/bin/sdcdb.exe", "sdcdb"
    msys2_bin "ucrt64/bin/sdcpp.exe", "sdcpp"
    msys2_bin "ucrt64/bin/sdld.exe", "sdld"
    msys2_bin "ucrt64/bin/sdld6808.exe", "sdld6808"
    msys2_bin "ucrt64/bin/sdldf8.exe", "sdldf8"
    msys2_bin "ucrt64/bin/sdldgb.exe", "sdldgb"
    msys2_bin "ucrt64/bin/sdldpdk.exe", "sdldpdk"
    msys2_bin "ucrt64/bin/sdldstm8.exe", "sdldstm8"
    msys2_bin "ucrt64/bin/sdldz80.exe", "sdldz80"
    msys2_bin "ucrt64/bin/sdnm.exe", "sdnm"
    msys2_bin "ucrt64/bin/sdobjcopy.exe", "sdobjcopy"
    msys2_bin "ucrt64/bin/sdranlib.exe", "sdranlib"
    msys2_bin "ucrt64/bin/ucsim_51.exe", "ucsim_51"
    msys2_bin "ucrt64/bin/ucsim_f8.exe", "ucsim_f8"
    msys2_bin "ucrt64/bin/ucsim_i8051.exe", "ucsim_i8051"
    msys2_bin "ucrt64/bin/ucsim_m6800.exe", "ucsim_m6800"
    msys2_bin "ucrt64/bin/ucsim_m68hc08.exe", "ucsim_m68hc08"
    msys2_bin "ucrt64/bin/ucsim_mos6502.exe", "ucsim_mos6502"
    msys2_bin "ucrt64/bin/ucsim_pdk.exe", "ucsim_pdk"
    msys2_bin "ucrt64/bin/ucsim_rxk.exe", "ucsim_rxk"
    msys2_bin "ucrt64/bin/ucsim_stm8.exe", "ucsim_stm8"
    msys2_bin "ucrt64/bin/ucsim_tlcs.exe", "ucsim_tlcs"
    msys2_bin "ucrt64/bin/ucsim_xa.exe", "ucsim_xa"
    msys2_bin "ucrt64/bin/ucsim_z80.exe", "ucsim_z80"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/sdcc --version 2>&1")
  end
end
