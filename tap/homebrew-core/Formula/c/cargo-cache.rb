class CargoCache < Formula
  desc "Display information on the cargo cache, plus optional cache pruning"
  homepage "https://github.com/matthiaskrgr/cargo-cache"
  url "https://github.com/matthiaskrgr/cargo-cache/archive/refs/tags/0.8.3.tar.gz"
  sha256 "d0f71214d17657a27e26aef6acf491bc9e760432a9bc15f2571338fcc24800e4"
  license any_of: ["Apache-2.0", "MIT"]
  head "https://github.com/matthiaskrgr/cargo-cache.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "f923b10e4a0b133868ed3bc8c5a50d7aeb4e575b412c79282564838109c55e22"
  end

  depends_on "rust" => :build
  depends_on "rustup" => :test

  on_linux do
    depends_on "zlib-ng-compat"
  end

  # The test downloads the beta toolchain using rustup.
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

    output = shell_output(bin/"cargo-cache")
    assert_match "0 installed binaries:", output

    assert_match version.to_s, shell_output("#{bin}/cargo-cache --version")
  end
end
