class Wassette < Formula
  desc "Security-oriented runtime that runs WebAssembly Components via MCP"
  homepage "https://microsoft.github.io/wassette/"
  url "https://github.com/microsoft/wassette/archive/refs/tags/v0.8.0.tar.gz"
  sha256 "ed6ae36056fed2dc11f734623ca97f74159f648367b12c2d49c559b126f27ac0"
  license "MIT"
  head "https://github.com/microsoft/wassette.git", branch: "main"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "49c9f9be30cb6a222d1e485b033995370219844bd79f03ffa94537d7bce40d73"
  end

  depends_on "pkgconf" => :build
  depends_on "rust" => :build

  deny_network_access!

  def crate_path = "crates/wassette-mcp-server"

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args, "--manifest-path", "#{crate_path}/Cargo.toml"
  end

  def install
    system "cargo", "install", *std_cargo_args(path: crate_path)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/wassette --version")

    output = shell_output("#{bin}/wassette component list")
    assert_equal "0", JSON.parse(output)["total"].to_s
  end
end
