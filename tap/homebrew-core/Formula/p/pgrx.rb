class Pgrx < Formula
  desc "Build Postgres Extensions with Rust"
  homepage "https://github.com/pgcentralfoundation/pgrx"
  url "https://github.com/pgcentralfoundation/pgrx/archive/refs/tags/v0.19.3.tar.gz"
  sha256 "3b6e931400c5bd40cd65e100155227e02b670a5e776d8649a776101a52f70f82"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "3fd6897eb8dfe1e5f47df61b691b43f586db6fef0dc392b00bedf4aa17e5d70f"
  end

  depends_on "pkgconf" => :build
  depends_on "rust" => :build
  depends_on "rustup" => :test

  on_linux do
    depends_on "openssl@4"
  end

  allow_network_access! :test

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "cargo-pgrx")
  end

  test do
    # Show that we can use a different toolchain than the one provided by the `rust` formula.
    # https://github.com/Homebrew/homebrew-core/pull/134074#pullrequestreview-1484979359
    ENV.prepend_path "PATH", formula_opt_bin("rustup")
    system "rustup", "set", "profile", "minimal"
    system "rustup", "default", "beta"

    system "cargo", "pgrx", "new", "my_extension"
    assert_path_exists testpath/"my_extension/my_extension.control"
  end
end
