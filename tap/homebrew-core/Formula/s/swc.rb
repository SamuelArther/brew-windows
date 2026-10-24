class Swc < Formula
  desc "Super-fast Rust-based JavaScript/TypeScript compiler"
  homepage "https://swc.rs"
  url "https://github.com/swc-project/swc/archive/refs/tags/v1.16.20.tar.gz"
  sha256 "26d8ca8a93c2ee15606340cfa794d97bcbb8b26bb668edd8ccdeff8a0202a649"
  license "Apache-2.0"
  head "https://github.com/swc-project/swc.git", branch: "main"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "cae336642b73695c5499b63972703341a9770f82bab0c0d4a1a2443e13fc4ef4"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    # `-Zshare-generics=y` flag is only supported on nightly Rust
    rm ".cargo/config.toml"

    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
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
