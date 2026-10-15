class Tinysearch < Formula
  desc "Tiny, full-text search engine for static websites built with Rust and Wasm"
  homepage "https://github.com/tinysearch/tinysearch"
  url "https://github.com/tinysearch/tinysearch/releases/download/v0.11.1/tinysearch-v0.11.1-x86_64-pc-windows-msvc.zip"
  sha256 "5789b4fbe2152452a55693a8bc7721e5a55c55b765fd3cf6ff8b53b8fe0997b5"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/tinysearch/tinysearch/archive/refs/tags/v0.11.1.tar.gz"
  end

  def install
    bin.install "tinysearch.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tinysearch --version 2>&1")
  end
end
