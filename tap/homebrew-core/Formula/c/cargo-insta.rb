class CargoInsta < Formula
  desc "Snapshot testing CLI for Rust"
  homepage "https://insta.rs"
  url "https://github.com/mitsuhiko/insta/releases/download/1.49.0/cargo-insta-x86_64-pc-windows-msvc.zip"
  sha256 "bf6374b054f596d75b5bb203010394a340cdb44d4150cd03560564fbc35d8156"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mitsuhiko/insta/archive/refs/tags/1.49.0.tar.gz"
  end

  def install
    bin.install "cargo-insta.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cargo-insta --version 2>&1")
  end
end
