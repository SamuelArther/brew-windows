class Opencc < Formula
  desc "Simplified-traditional Chinese conversion tool"
  homepage "https://opencc.byvoid.com/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-opencc-1.3.1-1-any.pkg.tar.zst"
  sha256 "4cdee6cfcb007b547802c0a137084f9dccf60afdd87266089176f313cbad740e"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/BYVoid/OpenCC/archive/refs/tags/ver.1.4.2.tar.gz"
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

  resource "mingw-w64-ucrt-x86_64-marisa" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-marisa-0.2.7-1-any.pkg.tar.zst"
    sha256 "60f1c815fc31a098fee0fc01cbb36b3926fc1184c4c25b33fa996e6f68d889a1"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/opencc.exe", "opencc"
    msys2_bin "ucrt64/bin/opencc_dict.exe", "opencc_dict"
    msys2_bin "ucrt64/bin/opencc_phrase_extract.exe", "opencc_phrase_extract"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/opencc --version 2>&1")
  end
end
