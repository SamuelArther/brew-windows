class WasmTools < Formula
  desc "Low level tooling for WebAssembly in Rust"
  homepage "https://github.com/bytecodealliance/wasm-tools"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-wasm-tools-1.262.0-1-any.pkg.tar.zst"
  sha256 "d52efc40655fbc18ed2eafdf62748fde0a6a07530a818b86f91945867de557c2"
  license any_of: [
    { "Apache-2.0" => { with: "LLVM-exception" } },
    "Apache-2.0",
    "MIT",
  ]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/bytecodealliance/wasm-tools/archive/refs/tags/v1.262.0.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/wasm-tools.exe", "wasm-tools"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/wasm-tools --version 2>&1")
  end
end
