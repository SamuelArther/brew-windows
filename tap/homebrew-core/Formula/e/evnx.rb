class Evnx < Formula
  desc "Comprehensive CLI tool for managing .env files"
  homepage "https://evnx.dev"
  url "https://github.com/urwithajit9/evnx/archive/refs/tags/v0.15.0.tar.gz"
  sha256 "51ea8c32699cc2eda5b5f7cc81e1e47a5d64a0287d9c443fdf695ad0ed267a9b"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a3b1dc714aa0405ba33023ba695a0f0ac34cbb1c03e09dd978dbbf6368e132d3"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/evnx --version")

    system bin/"evnx", "init", "--yes"
    assert_path_exists testpath/".env"
    assert_match "All checks passed", shell_output("#{bin}/evnx validate")

    (testpath/".env.example").append_lines "API_KEY="
    assert_match "Validation failed", shell_output("#{bin}/evnx validate 2>&1", 1)
  end
end
