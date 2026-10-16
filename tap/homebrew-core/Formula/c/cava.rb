class Cava < Formula
  desc "Console-based Audio Visualizer for ALSA"
  homepage "https://github.com/karlstav/cava"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cava-0.10.7-1-any.pkg.tar.zst"
  sha256 "1fec18e1d87cdfca3004309bea7cbe2e568bc6bbd3bbb47a14c04d87058b7fb5"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/karlstav/cava/archive/refs/tags/1.0.0.tar.gz"
  end

  resource "mingw-w64-ucrt-x86_64-SDL2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-SDL2-2.32.10-1-any.pkg.tar.zst"
    sha256 "9e77124b14bedaafbfb44ac26e5fb43890184db23292cebf2251b2178fad6693"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-fftw" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-fftw-3.3.11-2-any.pkg.tar.zst"
    sha256 "8bcffc286600ae011726e9356f19ea8afba0a622427c77e89b5a09ecce252d95"
  end

  resource "mingw-w64-ucrt-x86_64-glew" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-glew-2.3.1-4-any.pkg.tar.zst"
    sha256 "69cf8d2d95edc36f481d44e2a926520bd43d3aa098246e46b57abe5d11364065"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libgomp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgomp-16.2.0-6-any.pkg.tar.zst"
    sha256 "809b95086d8d3ba145e041bb36d5756b4407d1d97c0524ef731d3b94548faa69"
  end

  resource "mingw-w64-ucrt-x86_64-libiconv" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libiconv-1.19-1-any.pkg.tar.zst"
    sha256 "9a500f38c2b91808741c62fae746b3e9110b33a1ecf5c30fa0c66dbedddf7e16"
  end

  resource "mingw-w64-ucrt-x86_64-libquadmath" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libquadmath-16.2.0-6-any.pkg.tar.zst"
    sha256 "7b553c0b6dd9cf2f676c0c6812aa359495940dbdd7347f47be1dbd75d29cd611"
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

  resource "mingw-w64-ucrt-x86_64-vulkan-loader" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-vulkan-loader-1~1.4.363.0-1-any.pkg.tar.zst"
    sha256 "065076fe13d39ab97eea4740596426d5ae741b5044e48e1c9495d04835c92dbe"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/cava.exe", "cava"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cava -version 2>&1")
  end
end
