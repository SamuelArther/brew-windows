class Swc < Formula
  desc "Super-fast Rust-based JavaScript/TypeScript compiler"
  homepage "https://swc.rs"
  url "https://github.com/swc-project/swc/archive/refs/tags/v1.16.18.tar.gz"
  sha256 "70d900ec369808097018193d18a3888d37266740edf720da79688623560c81d1"
  license "Apache-2.0"
  head "https://github.com/swc-project/swc.git", branch: "main"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "e1955f1852d67941dd1b5b7d04b2006d80ab70460e43e0448c043e194a6e5f38"
  end

  depends_on "rust" => :build

  def install
    # `-Zshare-generics=y` flag is only supported on nightly Rust
    rm ".cargo/config.toml"

    system "cargo", "install", *std_cargo_args(path: "crates/swc_cli_impl")
  end

  test do
    (testpath/"test.js").write <<~JS
      const x = () => 42;
    JS

    system bin/"swc", "compile", "test.js", "--out-file", "test.out.js"
    assert_path_exists testpath/"test.out.js"

    # An abort ends a Windows process with STATUS_STACK_BUFFER_OVERRUN (0xC0000409), not SIGABRT's 134.
    output = shell_output("#{bin}/swc lint 2>&1", OS.windows? ? -1_073_740_791 : 134)
    assert_match "Lint command is not yet implemented", output
  end
end
