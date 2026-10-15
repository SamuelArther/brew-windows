class Hwloc < Formula
  desc "Portable abstraction of the hierarchical topology of modern architectures"
  homepage "https://www.open-mpi.org/projects/hwloc/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-hwloc-2.15.0-1-any.pkg.tar.zst"
  sha256 "ddd653c07042172cc0fdb462f18313d5369d2c23c5899a0a10d4f3d8ce2bd62c"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://www.open-mpi.org/software/hwloc/current/downloads/latest_release.txt"
    regex(/(\d+\.\d+\.\d+)/i)
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
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

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/hwloc-annotate.exe", "hwloc-annotate"
    msys2_bin "ucrt64/bin/hwloc-bind.exe", "hwloc-bind"
    msys2_bin "ucrt64/bin/hwloc-calc.exe", "hwloc-calc"
    msys2_bin "ucrt64/bin/hwloc-diff.exe", "hwloc-diff"
    msys2_bin "ucrt64/bin/hwloc-distrib.exe", "hwloc-distrib"
    msys2_bin "ucrt64/bin/hwloc-gather-cpuid.exe", "hwloc-gather-cpuid"
    msys2_bin "ucrt64/bin/hwloc-info.exe", "hwloc-info"
    msys2_bin "ucrt64/bin/hwloc-ls.exe", "hwloc-ls"
    msys2_bin "ucrt64/bin/hwloc-patch.exe", "hwloc-patch"
    msys2_bin "ucrt64/bin/lstopo-no-graphics.exe", "lstopo-no-graphics"
    msys2_bin "ucrt64/bin/lstopo.exe", "lstopo"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/hwloc-annotate --version 2>&1")
  end
end
