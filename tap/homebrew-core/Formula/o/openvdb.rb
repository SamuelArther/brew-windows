class Openvdb < Formula
  desc "Sparse volumetric data processing toolkit"
  homepage "https://www.openvdb.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openvdb-13.0.0-1-any.pkg.tar.zst"
  sha256 "709482743f0fb3e99223f52268f46f75fc950f9ab56b511420a93975054d90cb"
  license "MPL-2.0"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/AcademySoftwareFoundation/openvdb/archive/refs/tags/v13.1.0.tar.gz"
  end

  resource "mingw-w64-ucrt-x86_64-blosc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-blosc-1.21.6-3-any.pkg.tar.zst"
    sha256 "6d04706365c8ae021333a8213cb514ec8ef149eecfffa0370a05201087653a76"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-hwloc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-hwloc-2.15.0-1-any.pkg.tar.zst"
    sha256 "ddd653c07042172cc0fdb462f18313d5369d2c23c5899a0a10d4f3d8ce2bd62c"
  end

  resource "mingw-w64-ucrt-x86_64-imath" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-imath-3.2.3-3-any.pkg.tar.zst"
    sha256 "7a2e2175d5bd126c277e1eb0a059004f8750918e7dbbd447554c7b46dca34d60"
  end

  resource "mingw-w64-ucrt-x86_64-jemalloc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-jemalloc-5.4.0-1-any.pkg.tar.zst"
    sha256 "7494cf2d8c6d82b4159c5bd6bc127ce24c453cd08271457e267bc44076fddaa6"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
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

  resource "mingw-w64-ucrt-x86_64-lz4" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lz4-1.10.0-1-any.pkg.tar.zst"
    sha256 "00a807df81a4b7e82445178b8625962ed592cf1f0a08a8e04c8b9338c4362638"
  end

  resource "mingw-w64-ucrt-x86_64-snappy" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-snappy-1.3.1-1-any.pkg.tar.zst"
    sha256 "a4037212ff1e8a4c12af0b7b5443d3abb46ca7e30849a45160db90add74b54c8"
  end

  resource "mingw-w64-ucrt-x86_64-tbb" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tbb-2023.1.0-1-any.pkg.tar.zst"
    sha256 "1dcd85163ef30c1ce313d72a9f8df90188da2a1b74effa5bb291250d3f7b0f67"
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
    msys2_bin "ucrt64/bin/nanovdb_print.exe", "nanovdb_print"
    msys2_bin "ucrt64/bin/nanovdb_validate.exe", "nanovdb_validate"
    msys2_bin "ucrt64/bin/vdb_print.exe", "vdb_print"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/nanovdb_print --version 2>&1")
  end
end
