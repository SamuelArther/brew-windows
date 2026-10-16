class Glibc < Formula
  desc "C library for Windows programs (MinGW-w64 UCRT runtime under glibc's library names)"
  homepage "https://www.mingw-w64.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-crt-14.0.0.r426.g4564ee4b5-1-any.pkg.tar.zst"
  version "14.0.0.r426"
  sha256 "5d1c1f2c3859e690181d9ebcada1640a6744ab8dd5643b4bdbc7fe258af95037"
  license all_of: ["ZPL-2.1", "MIT", "LGPL-2.1-or-later"]

  # glibc is the C library of GNU/Linux. On Windows the C library is Microsoft's Universal C Runtime
  # (ucrtbase.dll, part of Windows 10 and 11), and the GNU toolchain's view of it is MinGW-w64: its
  # C runtime startup objects and import libraries, the C/POSIX headers, winpthreads (POSIX
  # threads, clock_gettime, nanosleep) and dlfcn-win32 (dlopen/dlsym on LoadLibrary). This keg
  # carries those from MSYS2's UCRT64 packages and adds the library names glibc provides, so
  # builds that link -lc, -lm, -lpthread, -ldl or -lrt against glibc find the Windows equivalents:
  #   libc  -> the UCRT import library (libucrt.a)
  #   libm  -> MinGW-w64's libm (math lives in the UCRT itself)
  #   libpthread, librt -> winpthreads
  #   libdl -> dlfcn-win32

  keg_only "it would shadow the C runtime of the GCC toolchain"

  livecheck do
    url "https://repo.msys2.org/mingw/ucrt64/"
    regex(/href=.*?mingw-w64-ucrt-x86_64-crt[._-]v?(\d+(?:\.\d+)+(?:\.r\d+)?)/i)
  end

  resource "mingw-w64-ucrt-x86_64-headers" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-headers-14.0.0.r426.g4564ee4b5-1-any.pkg.tar.zst"
    sha256 "b81c19e278ba0fc9f6ad0326d4aef64aea93053a6272916fb4c07069ac0215d0"
  end

  resource "mingw-w64-ucrt-x86_64-winpthreads" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-winpthreads-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a29b3919ef25c1d1671b8899bd947903bbf8733e7ada4a71be5bf02553dceab3"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-dlfcn" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-dlfcn-1.4.2-1-any.pkg.tar.zst"
    sha256 "892003898aa5e689c9cb10b0b73760cd93abd20b36009d084bd68cb8890c76a8"
  end

  def install
    install_tree
    install_resource "mingw-w64-ucrt-x86_64-headers", "libexec"
    install_resource "mingw-w64-ucrt-x86_64-winpthreads", "libexec"
    install_resource "mingw-w64-ucrt-x86_64-libwinpthread", "libexec"
    install_resource "mingw-w64-ucrt-x86_64-dlfcn", "libexec"
    install_library
    # glibc's library names.
    cp lib/"libucrt.a", lib/"libc.a"
    cp lib/"libpthread.a", lib/"librt.a"
    cp lib/"libpthread.dll.a", lib/"librt.dll.a" if (lib/"libpthread.dll.a").exist?
  end

  test do
    # No system C compiler on Windows to build a test program; check the C library's pieces.
    assert_match "printf", (include/"stdio.h").read
    assert_match "pthread_create", (include/"pthread.h").read
    assert_match "dlopen", (include/"dlfcn.h").read
    %w[libc.a libm.a libpthread.a librt.a libmingw32.a crt2.o].each { |f| assert_path_exists lib/f }
    assert_path_exists bin/"libwinpthread-1.dll"
  end
end
