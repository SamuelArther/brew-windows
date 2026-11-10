class Cot < Formula
  desc "Rust web framework for lazy developers"
  homepage "https://cot.rs"
  url "https://github.com/cot-rs/cot/archive/refs/tags/cot-v0.8.0.tar.gz"
  sha256 "89530ac4e9b49df21679b55f9d7fb392bbc9b167da9707fbd6862365f85c009b"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "cb6db83a35d34d740cf757852d6a4c5d42c8032b7256dee0af16c6f39c7737eb"
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
