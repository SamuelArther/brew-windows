class Cot < Formula
  desc "Rust web framework for lazy developers"
  homepage "https://cot.rs"
  url "https://github.com/cot-rs/cot/archive/refs/tags/cot-v0.7.0.tar.gz"
  sha256 "8d84e6645e05b213c64e21de4e21200e04cbdcdf934d7b8aa40fa885a1ee74ef"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "55d1fc2c4fa571a63c903a5910c5ebaf4f48ccf0ebfba9c592794dfe86f16308"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "cot-cli")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cot --version")

    system bin/"cot", "new", "test-project"
    assert_path_exists testpath/"test-project/Cargo.toml"
  end
end
