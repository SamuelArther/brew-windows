class Toml2json < Formula
  desc "Convert TOML to JSON"
  homepage "https://github.com/woodruffw/toml2json"
  url "https://github.com/woodruffw/toml2json/archive/refs/tags/v1.4.0.tar.gz"
  sha256 "70c9bf872f22d22691d41eeb06442a0fddf4b341ddebfadf8734d1e68e0174ce"
  license "MIT"
  head "https://github.com/woodruffw/toml2json.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "5942e362e118cd958ee0adeb274c880ee44b9457fc02cf22c2acfce5352799f9"
  end

  depends_on "rust" => :build

  conflicts_with "remarshal", because: "both install `toml2json` binaries"

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    out = pipe_output(bin/"toml2json", 'wow = "amazing"')
    json = JSON.parse(out)
    assert_equal "amazing", json.fetch("wow")
  end
end
