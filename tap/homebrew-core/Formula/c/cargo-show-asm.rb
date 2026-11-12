class CargoShowAsm < Formula
  desc "Show assembly, LLVM-IR, MIR, and WASM generated for Rust code"
  homepage "https://github.com/pacak/cargo-show-asm"
  url "https://github.com/pacak/cargo-show-asm/archive/refs/tags/0.2.65.tar.gz"
  sha256 "c6550e496cd0ae89f36d4be2741e832688a3c4e80b684f5506a622549cfeddb8"
  license any_of: ["MIT", "Apache-2.0"]
  head "https://github.com/pacak/cargo-show-asm.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "af605c7972fbeef37d40994c9cee63f1a2dd665c42d02c362d4a5ab4074547c8"
  end

  depends_on "rust" => :build
  depends_on "rustup" => :test

  allow_network_access! :test

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    ENV.prepend_path "PATH", formula_opt_bin("rustup")
    system "rustup", "set", "profile", "minimal"
    system "rustup", "default", "stable"
    system "cargo", "new", "test_asm", "--lib"
    cd "test_asm" do
      rm testpath/"test_asm/src/lib.rs"
      (testpath/"test_asm/src/lib.rs").write <<~RUST
        #[inline(never)]
        pub fn hello() -> &'static str {
            "Hello"
        }
      RUST
      output = shell_output("#{bin}/cargo-asm asm --lib hello 2>&1")
      assert_match "test_asm::hello", output
    end
  end
end
