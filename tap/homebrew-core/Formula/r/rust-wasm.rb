class RustWasm < Formula
  desc "Rust standard library and sysroot for WebAssembly targets"
  homepage "https://www.rust-lang.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-rust-wasm-1.99.0-2-any.pkg.tar.zst"
  sha256 "bdeff6903f85ee3108640335e6fedea84d3e53782eca2b817884f774ca3c40e7"
  license any_of: ["Apache-2.0", "MIT"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://static.rust-lang.org/dist/rustc-1.99.0-src.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists lib/"rustlib/wasm32-unknown-unknown/lib/libaddr2line-e2fdb4a47176fbf0.rlib"
  end
end
