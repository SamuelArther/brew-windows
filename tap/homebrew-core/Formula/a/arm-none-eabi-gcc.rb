class ArmNoneEabiGcc < Formula
  desc "GNU compiler collection for arm-none-eabi"
  homepage "https://gcc.gnu.org"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-arm-none-eabi-gcc-16.1.0-1-any.pkg.tar.zst"
  sha256 "69fed932ad228eeef090be6242ab8fe57c45a9a7ec691876ada052eb6ea318cb"
  license "GPL-3.0-or-later" => { with: "GCC-exception-3.1" }

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://ftpmirror.gnu.org/gcc/gcc-16.2.0/gcc-16.2.0.tar.xz"
  end

  resource "mingw-w64-ucrt-x86_64-arm-none-eabi-binutils" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-arm-none-eabi-binutils-2.47-1-any.pkg.tar.zst"
    sha256 "b6bd237b4905f693fbe3995237f6e38dc69946fbcc867f2b9c20d0dfdeb8b7cf"
  end

  resource "mingw-w64-ucrt-x86_64-arm-none-eabi-newlib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-arm-none-eabi-newlib-4.6.0.20260123-1-any.pkg.tar.zst"
    sha256 "196cff027d19f3bf1929858b33ff463a8e4dc7b5a5f9341db785f63ec89241ff"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-gmp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gmp-6.3.0-2-any.pkg.tar.zst"
    sha256 "e82a75968a556484a50084578238a84eb60fb93e34986fd6695c537975bd39ea"
  end

  resource "mingw-w64-ucrt-x86_64-isl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-isl-0.28-1-any.pkg.tar.zst"
    sha256 "8594e01a253d5a72646c586cf5a1626c1b1c2be7c30060fe67abd9bebe3fe223"
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

  resource "mingw-w64-ucrt-x86_64-mpc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-mpc-1.4.1-1-any.pkg.tar.zst"
    sha256 "f06556e811c711ce91609484e31d358e4f495811ba29813bf2284a3b236d82ce"
  end

  resource "mingw-w64-ucrt-x86_64-mpfr" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-mpfr-4.2.2-3-any.pkg.tar.zst"
    sha256 "6b70a275d2ec75c70aa50a236c218e6b7ee9a5ab4c6d52cb0b580267b7d200fe"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
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
    msys2_bin "ucrt64/bin/arm-none-eabi-c++.exe", "arm-none-eabi-c++"
    msys2_bin "ucrt64/bin/arm-none-eabi-cpp.exe", "arm-none-eabi-cpp"
    msys2_bin "ucrt64/bin/arm-none-eabi-g++.exe", "arm-none-eabi-g++"
    msys2_bin "ucrt64/bin/arm-none-eabi-gcc-16.1.0.exe", "arm-none-eabi-gcc-16.1.0"
    msys2_bin "ucrt64/bin/arm-none-eabi-gcc-ar.exe", "arm-none-eabi-gcc-ar"
    msys2_bin "ucrt64/bin/arm-none-eabi-gcc-nm.exe", "arm-none-eabi-gcc-nm"
    msys2_bin "ucrt64/bin/arm-none-eabi-gcc-ranlib.exe", "arm-none-eabi-gcc-ranlib"
    msys2_bin "ucrt64/bin/arm-none-eabi-gcc.exe", "arm-none-eabi-gcc"
    msys2_bin "ucrt64/bin/arm-none-eabi-gcov-dump.exe", "arm-none-eabi-gcov-dump"
    msys2_bin "ucrt64/bin/arm-none-eabi-gcov-tool.exe", "arm-none-eabi-gcov-tool"
    msys2_bin "ucrt64/bin/arm-none-eabi-gcov.exe", "arm-none-eabi-gcov"
    msys2_bin "ucrt64/bin/arm-none-eabi-lto-dump.exe", "arm-none-eabi-lto-dump"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/arm-none-eabi-gcc --version 2>&1")
  end
end
