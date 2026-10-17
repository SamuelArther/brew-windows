class CargoFeaturesManager < Formula
  desc "TUI like cli tool to manage the features of your rust-project dependencies"
  homepage "https://tangled.org/tobinio.dev/cargo-features-manager"
  url "https://static.crates.io/crates/cargo-features-manager/cargo-features-manager-0.12.0.crate"
  sha256 "cbfa8ceb9b52aff46d6122c97752e1cc0a638c028e676159ebff11128f0cf333"
  license "MIT"
  head "https://tangled.org/tobinio.dev/cargo-features-manager.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "5a71080161bb8c89fbf4344c635bb06f9a5dad50c06982dee4a32bab9dd1b167"
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
    # Show that we can use a different toolchain than the one provided by the `rust` formula.
    # https://github.com/Homebrew/homebrew-core/pull/134074#pullrequestreview-1484979359
    ENV.prepend_path "PATH", formula_opt_bin("rustup")
    system "rustup", "set", "profile", "minimal"
    system "rustup", "default", "beta"

    crate = testpath/"demo-crate"
    mkdir crate do
      (crate/"Cargo.toml").write <<~TOML
        [package]
        name = "demo-crate"
        version = "0.1.0"

        [lib]
        path = "lib.rs"

        [dependencies]
        libc = "0.1"
        bear = "0.2"
      TOML

      (crate/"lib.rs").write "fn main() {}"

      output = shell_output("cargo features prune")
      assert_match "workspace [1]", output
      assert_match "libc [0/1]", output
    end
  end
end
