class CargoAuditable < Formula
  desc "Make production Rust binaries auditable"
  homepage "https://github.com/rust-secure-code/cargo-auditable"
  url "https://github.com/rust-secure-code/cargo-auditable/releases/download/v0.7.7/cargo-auditable-x86_64-pc-windows-msvc.zip"
  sha256 "717ba2d3ed7997c501e9987ee7c94bebf9d7219b18b185a03009420ec7029c7d"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/rust-secure-code/cargo-auditable/archive/refs/tags/v0.7.7.tar.gz"
  end

  def install
    bin.install "cargo-auditable.exe"
  end

  test do
    assert_path_exists bin/"cargo-auditable.exe"
  end
end
