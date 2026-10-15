class Gcc < Formula
  desc "GNU compiler collection"
  homepage "https://gcc.gnu.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gcc-16.2.0-6-any.pkg.tar.zst"
  sha256 "0290e91f5a9a8a3699bcea6221a899403bb9bd5d6f7dbd98c883d74e2ef36338"
  license "GPL-3.0-or-later" => { with: "GCC-exception-3.1" }

  # Native UCRT64 GCC toolchain (mingw-w64 targeting x86_64-w64-mingw32) with binutils.
  # Commands are exposed unversioned and with Homebrew's -16 suffix.
  # Programs linked to libstdc++/libgcc DLLs need -static (or libexec/ucrt64/bin on PATH) to run.
  # Very deep BREW_PREFIX paths can push C++ headers past Windows' 260-character MAX_PATH.

  resource "mingw-w64-ucrt-x86_64-binutils" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-binutils-2.47-4-any.pkg.tar.zst"
    sha256 "c18a40b871bfddb5da030f015003479ca85625afde1cb89f7313945e6151397d"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-crt" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-crt-14.0.0.r426.g4564ee4b5-1-any.pkg.tar.zst"
    sha256 "5d1c1f2c3859e690181d9ebcada1640a6744ab8dd5643b4bdbc7fe258af95037"
  end

  resource "mingw-w64-ucrt-x86_64-gettext-runtime" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gettext-runtime-1.0-1-any.pkg.tar.zst"
    sha256 "ba693dda4ac375af76ce481ff3a6e7481286546cc7dc6d56c7021dae34084157"
  end

  resource "mingw-w64-ucrt-x86_64-gmp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gmp-6.3.0-2-any.pkg.tar.zst"
    sha256 "e82a75968a556484a50084578238a84eb60fb93e34986fd6695c537975bd39ea"
  end

  resource "mingw-w64-ucrt-x86_64-headers" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-headers-14.0.0.r426.g4564ee4b5-1-any.pkg.tar.zst"
    sha256 "b81c19e278ba0fc9f6ad0326d4aef64aea93053a6272916fb4c07069ac0215d0"
  end

  resource "mingw-w64-ucrt-x86_64-isl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-isl-0.28-1-any.pkg.tar.zst"
    sha256 "8594e01a253d5a72646c586cf5a1626c1b1c2be7c30060fe67abd9bebe3fe223"
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

  resource "mingw-w64-ucrt-x86_64-windows-default-manifest" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-windows-default-manifest-20260815-1-any.pkg.tar.zst"
    sha256 "b39039f754a600cc0dec49df7d675a59a3cb2b63691d91b4ae60735ee7075eca"
  end

  resource "mingw-w64-ucrt-x86_64-winpthreads" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-winpthreads-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a29b3919ef25c1d1671b8899bd947903bbf8733e7ada4a71be5bf02553dceab3"
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
    msys2_bin "ucrt64/bin/gcc.exe", "gcc"
    msys2_bin "ucrt64/bin/g++.exe", "g++"
    msys2_bin "ucrt64/bin/c++.exe", "c++"
    msys2_bin "ucrt64/bin/cpp.exe", "cpp"
    msys2_bin "ucrt64/bin/gcc-ar.exe", "gcc-ar"
    msys2_bin "ucrt64/bin/gcc-nm.exe", "gcc-nm"
    msys2_bin "ucrt64/bin/gcc-ranlib.exe", "gcc-ranlib"
    msys2_bin "ucrt64/bin/gcov.exe", "gcov"
    msys2_bin "ucrt64/bin/gcov-dump.exe", "gcov-dump"
    msys2_bin "ucrt64/bin/gcov-tool.exe", "gcov-tool"
    msys2_bin "ucrt64/bin/x86_64-w64-mingw32-gcc.exe", "x86_64-w64-mingw32-gcc"
    msys2_bin "ucrt64/bin/x86_64-w64-mingw32-g++.exe", "x86_64-w64-mingw32-g++"
    msys2_bin "ucrt64/bin/x86_64-w64-mingw32-c++.exe", "x86_64-w64-mingw32-c++"
    msys2_bin "ucrt64/bin/addr2line.exe", "addr2line"
    msys2_bin "ucrt64/bin/ar.exe", "ar"
    msys2_bin "ucrt64/bin/as.exe", "as"
    msys2_bin "ucrt64/bin/c++filt.exe", "c++filt"
    msys2_bin "ucrt64/bin/dlltool.exe", "dlltool"
    msys2_bin "ucrt64/bin/elfedit.exe", "elfedit"
    msys2_bin "ucrt64/bin/gprof.exe", "gprof"
    msys2_bin "ucrt64/bin/ld.exe", "ld"
    msys2_bin "ucrt64/bin/nm.exe", "nm"
    msys2_bin "ucrt64/bin/objcopy.exe", "objcopy"
    msys2_bin "ucrt64/bin/objdump.exe", "objdump"
    msys2_bin "ucrt64/bin/ranlib.exe", "ranlib"
    msys2_bin "ucrt64/bin/readelf.exe", "readelf"
    msys2_bin "ucrt64/bin/size.exe", "size"
    msys2_bin "ucrt64/bin/strings.exe", "strings"
    msys2_bin "ucrt64/bin/strip.exe", "strip"
    msys2_bin "ucrt64/bin/windmc.exe", "windmc"
    msys2_bin "ucrt64/bin/windres.exe", "windres"
    msys2_bin "ucrt64/bin/gcc.exe", "gcc-16"
    msys2_bin "ucrt64/bin/g++.exe", "g++-16"
    msys2_bin "ucrt64/bin/c++.exe", "c++-16"
    msys2_bin "ucrt64/bin/cpp.exe", "cpp-16"
  end

  test do
    (testpath/"hello-c.c").write <<~C
      #include <stdio.h>
      int main() { puts("Hello, world!"); return 0; }
    C
    system bin/"gcc-16", "-o", "hello-c", "hello-c.c"
    assert_equal "Hello, world!\n", shell_output("./hello-c")
  end
end
