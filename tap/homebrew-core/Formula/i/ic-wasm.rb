class IcWasm < Formula
  desc "CLI tool for performing Wasm transformations specific to ICP canisters"
  homepage "https://github.com/dfinity/ic-wasm"
  url "https://github.com/dfinity/ic-wasm/releases/download/0.11.1/ic-wasm-x86_64-pc-windows-msvc.zip"
  sha256 "75e0d3619f1f480a86d21b0422e4093e1f99eb3c0a5b464e8ab78fea62a28381"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dfinity/ic-wasm/archive/refs/tags/0.11.1.tar.gz"
  end

  def install
    bin.install "ic-wasm.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ic-wasm --version 2>&1")
  end
end
