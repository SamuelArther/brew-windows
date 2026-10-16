class SpirvTools < Formula
  desc "API and commands for processing SPIR-V modules"
  homepage "https://github.com/KhronosGroup/SPIRV-Tools"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-spirv-tools-3~1.4.363.0-1-any.pkg.tar.zst"
  version "1.4.363.0"
  sha256 "d31d67fe918aeee21d1e930fd96360856a60146d23094e4ae8baf071a4049aaa"
  license "Apache-2.0"
  version_scheme 1

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/KhronosGroup/SPIRV-Tools/archive/refs/tags/vulkan-sdk-1.4.363.0.tar.gz"
    regex(/^(?:vulkan[._-])?sdk[._-]v?(\d+(?:\.\d+)+)$/i)
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

  def install
    msys2_install
    msys2_bin "ucrt64/bin/spirv-as.exe", "spirv-as"
    msys2_bin "ucrt64/bin/spirv-cfg.exe", "spirv-cfg"
    msys2_bin "ucrt64/bin/spirv-diff.exe", "spirv-diff"
    msys2_bin "ucrt64/bin/spirv-dis.exe", "spirv-dis"
    msys2_bin "ucrt64/bin/spirv-link.exe", "spirv-link"
    msys2_bin "ucrt64/bin/spirv-lint.exe", "spirv-lint"
    msys2_bin "ucrt64/bin/spirv-objdump.exe", "spirv-objdump"
    msys2_bin "ucrt64/bin/spirv-opt.exe", "spirv-opt"
    msys2_bin "ucrt64/bin/spirv-reduce.exe", "spirv-reduce"
    msys2_bin "ucrt64/bin/spirv-val.exe", "spirv-val"
  end

  test do
    system bin/"spirv-as", "--version"
  end
end
