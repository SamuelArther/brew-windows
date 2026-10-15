class CargoShear < Formula
  desc "Detect and remove unused dependencies from `Cargo.toml` in Rust projects"
  homepage "https://github.com/Boshen/cargo-shear"
  url "https://github.com/Boshen/cargo-shear/releases/download/v1.14.0/cargo-shear-x86_64-pc-windows-msvc.tar.gz"
  sha256 "2d3a25cb97cd13bcf57d8f0517e2049e179dec9a9f94b6486a787ca51c8f97e5"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Boshen/cargo-shear/archive/refs/tags/v1.14.0.tar.gz"
  end

  def install
    bin.install "cargo-shear.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cargo-shear --version 2>&1")
  end
end
