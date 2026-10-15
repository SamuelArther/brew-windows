class CargoCyclonedx < Formula
  desc "Creates CycloneDX Software Bill of Materials (SBOM) from Rust (Cargo) projects"
  homepage "https://cyclonedx.org/"
  url "https://github.com/CycloneDX/cyclonedx-rust-cargo/releases/download/cargo-cyclonedx-0.5.9/cargo-cyclonedx-x86_64-pc-windows-msvc.zip"
  version "0.5.9"
  sha256 "8750e00775661dcb75bc482c1a298839fd94e8a0c033b49905ba0f246ffed202"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/CycloneDX/cyclonedx-rust-cargo/archive/refs/tags/cargo-cyclonedx-0.5.9.tar.gz"
    regex(/^cargo-cyclonedx[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "cargo-cyclonedx.exe"
  end

  test do
    system bin/"cargo-cyclonedx", "--help"
  end
end
