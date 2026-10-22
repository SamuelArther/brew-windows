class Evnx < Formula
  desc "Comprehensive CLI tool for managing .env files"
  homepage "https://evnx.dev"
  url "https://github.com/urwithajit9/evnx/archive/refs/tags/v0.10.15.tar.gz"
  sha256 "ee2f298403d4abbb156f0113de8c7fd85b4599eb8bf095f5bd284b0c51c43dd3"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "2f7d98dbd55e96da2d7929c7a5845d9b0be0c21a51b9893f4ef40ca5bc5b3454"
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
