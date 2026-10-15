class Wabt < Formula
  desc "Web Assembly Binary Toolkit"
  homepage "https://github.com/WebAssembly/wabt"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-wabt-1.0.42-1-any.pkg.tar.zst"
  sha256 "42b6e65db0b580aa6c2aba1aaffb2bdc762d6831a3e41e7c6435d20d5bd09469"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/WebAssembly/wabt/releases/download/1.0.42/wabt-1.0.42.tar.xz"
    strategy :github_latest
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

  resource "mingw-w64-ucrt-x86_64-openssl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openssl-3.6.5-1-any.pkg.tar.zst"
    sha256 "773e021ebad83f14b5c103db467411e8ff064663e1c7e7f730e2be72f4117064"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/spectest-interp.exe", "spectest-interp"
    msys2_bin "ucrt64/bin/wasm-interp.exe", "wasm-interp"
    msys2_bin "ucrt64/bin/wasm-objdump.exe", "wasm-objdump"
    msys2_bin "ucrt64/bin/wasm-stats.exe", "wasm-stats"
    msys2_bin "ucrt64/bin/wasm-strip.exe", "wasm-strip"
    msys2_bin "ucrt64/bin/wasm-validate.exe", "wasm-validate"
    msys2_bin "ucrt64/bin/wasm2c.exe", "wasm2c"
    msys2_bin "ucrt64/bin/wasm2wat.exe", "wasm2wat"
    msys2_bin "ucrt64/bin/wast2json.exe", "wast2json"
    msys2_bin "ucrt64/bin/wat-desugar.exe", "wat-desugar"
    msys2_bin "ucrt64/bin/wat2wasm.exe", "wat2wasm"
  end

  test do
    (testpath/"sample.wast").write("(module (memory 1) (func))")
    system bin/"wat2wasm", testpath/"sample.wast"
  end
end
