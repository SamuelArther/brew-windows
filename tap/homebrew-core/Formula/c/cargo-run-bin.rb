class CargoRunBin < Formula
  desc "Build, cache, and run binaries from Cargo.toml to avoid global installs"
  homepage "https://dustinblackman.com/posts/why-does-everyone-install-crates-globally/"
  url "https://github.com/dustinblackman/cargo-run-bin/archive/refs/tags/v1.7.4.tar.gz"
  sha256 "fd492430a60ca488ad8c356f9c6389426f3fbcd59658e5b721855e171cb62841"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "f28357e6b48ae2db2e98e6e13b960d3e380336cb2f1a810dffebe288eb08b641"
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
    assert_equal "cargo-run-bin #{version}", shell_output("#{bin}/cargo-bin -V").strip

    # Show that we can use a different toolchain than the one provided by the `rust` formula.
    # https://github.com/Homebrew/homebrew-core/pull/134074#pullrequestreview-1484979359
    ENV.prepend_path "PATH", formula_opt_bin("rustup")
    system "rustup", "set", "profile", "minimal"
    system "rustup", "default", "beta"

    (testpath/"Cargo.toml").write <<~TOML
      [package]
      name = "homebrew_test"
      version = "0.1.0"
      edition = "2021"

      [[bin]]
      name = "homebrew_test"
      path = "src/main.rs"

      [package.metadata.bin]
      cargo-nextest = { version = "0.9.57", locked = true }
    TOML

    (testpath/"src/main.rs").write <<~RUST
      fn main() {
          println!("Hello, world!");
      }
    RUST

    system "cargo", "build"
    system bin/"cargo-bin", "--install"
    system bin/"cargo-bin", "--sync-aliases"

    assert_match <<~TOML, File.read(testpath/".cargo/config.toml")
      [alias]
      nextest = ["bin", "cargo-nextest"]
    TOML

    assert_match "next-generation test runner", shell_output("cargo nextest --help")
  end
end
