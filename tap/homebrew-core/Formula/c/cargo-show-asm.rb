class CargoShowAsm < Formula
  desc "Show assembly, LLVM-IR, MIR, and WASM generated for Rust code"
  homepage "https://github.com/pacak/cargo-show-asm"
  url "https://github.com/pacak/cargo-show-asm/archive/refs/tags/0.2.64.tar.gz"
  sha256 "14faeada9a44344c1b45788e794f456eb0632a8e34621eb4c2b6064094e1823e"
  license any_of: ["MIT", "Apache-2.0"]
  head "https://github.com/pacak/cargo-show-asm.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "b848add4a6db541e3357ee85892d6f7f3f0bdfb5b5da48621e6cc757773aa9d3"
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
