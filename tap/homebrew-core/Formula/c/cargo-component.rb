class CargoComponent < Formula
  desc "Create WebAssembly components based on the component model proposal"
  homepage "https://github.com/bytecodealliance/cargo-component"
  url "https://github.com/bytecodealliance/cargo-component/archive/refs/tags/v0.21.1.tar.gz"
  sha256 "04ded8443b34687641d0bf01fa682ce46c1a9300af3f13ea5cf1bf5487d6f8b1"
  license "Apache-2.0"
  head "https://github.com/bytecodealliance/cargo-component.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "6f8c8ac4fb67082a286fd9cf7e7d981849ae23c5c17c335f8fa51bd57036e4e3"
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

    system "cargo", "component", "new", "--lib", "brew-test"
    assert_path_exists testpath/"brew-test/wit/world.wit"
  end
end
