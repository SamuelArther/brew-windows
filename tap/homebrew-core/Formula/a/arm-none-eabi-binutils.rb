class ArmNoneEabiBinutils < Formula
  desc "GNU Binutils for arm-none-eabi cross development"
  homepage "https://www.gnu.org/software/binutils/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-arm-none-eabi-binutils-2.47-1-any.pkg.tar.zst"
  sha256 "b6bd237b4905f693fbe3995237f6e38dc69946fbcc867f2b9c20d0dfdeb8b7cf"
  license "GPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://ftpmirror.gnu.org/binutils/binutils-2.47.tar.bz2"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
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

  resource "mingw-w64-ucrt-x86_64-zstd" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zstd-1.5.7-2-any.pkg.tar.zst"
    sha256 "dbdb8427280046a2b41697780aa4c52983b708082b0da4755951dc3bea96ca89"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/arm-none-eabi-addr2line.exe", "arm-none-eabi-addr2line"
    msys2_bin "ucrt64/bin/arm-none-eabi-ar.exe", "arm-none-eabi-ar"
    msys2_bin "ucrt64/bin/arm-none-eabi-as.exe", "arm-none-eabi-as"
    msys2_bin "ucrt64/bin/arm-none-eabi-c++filt.exe", "arm-none-eabi-c++filt"
    msys2_bin "ucrt64/bin/arm-none-eabi-elfedit.exe", "arm-none-eabi-elfedit"
    msys2_bin "ucrt64/bin/arm-none-eabi-gprof.exe", "arm-none-eabi-gprof"
    msys2_bin "ucrt64/bin/arm-none-eabi-ld.bfd.exe", "arm-none-eabi-ld.bfd"
    msys2_bin "ucrt64/bin/arm-none-eabi-ld.exe", "arm-none-eabi-ld"
    msys2_bin "ucrt64/bin/arm-none-eabi-nm.exe", "arm-none-eabi-nm"
    msys2_bin "ucrt64/bin/arm-none-eabi-objcopy.exe", "arm-none-eabi-objcopy"
    msys2_bin "ucrt64/bin/arm-none-eabi-objdump.exe", "arm-none-eabi-objdump"
    msys2_bin "ucrt64/bin/arm-none-eabi-ranlib.exe", "arm-none-eabi-ranlib"
    msys2_bin "ucrt64/bin/arm-none-eabi-readelf.exe", "arm-none-eabi-readelf"
    msys2_bin "ucrt64/bin/arm-none-eabi-size.exe", "arm-none-eabi-size"
    msys2_bin "ucrt64/bin/arm-none-eabi-strings.exe", "arm-none-eabi-strings"
    msys2_bin "ucrt64/bin/arm-none-eabi-strip.exe", "arm-none-eabi-strip"
  end

  test do
    (testpath/"test-s.s").write <<~ASM
      .section .text
      .globl _start
      _start:
          mov r1, #0
          mov r2, #1
          svc #0x80
    ASM

    system bin/"arm-none-eabi-as", "-o", "test-s.o", "test-s.s"
    assert_match "file format elf32-littlearm",
                 shell_output("#{bin}/arm-none-eabi-objdump -a test-s.o")
    assert_match "f()", shell_output("#{bin}/arm-none-eabi-c++filt _Z1fv")
  end
end
