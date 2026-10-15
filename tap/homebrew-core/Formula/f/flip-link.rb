class FlipLink < Formula
  desc "Adds zero-cost stack overflow protection to your embedded programs"
  homepage "https://github.com/knurling-rs/flip-link"
  url "https://github.com/knurling-rs/flip-link/releases/download/v0.1.12/flip-link-x86_64-pc-windows-msvc.zip"
  sha256 "b64d6bed1872d5cd1057b1e1e44ebb1e18d18bf043a7e3479adad7962c5ba1bd"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/knurling-rs/flip-link/archive/refs/tags/v0.1.12.tar.gz"
  end

  def install
    bin.install "flip-link.exe"
  end

  test do
    system bin/"flip-link", "--help"
  end
end
