class Evnx < Formula
  desc "Comprehensive CLI tool for managing .env files"
  homepage "https://evnx.dev"
  url "https://github.com/urwithajit9/evnx/archive/refs/tags/v0.10.8.tar.gz"
  sha256 "f73ce6ecdc0f54684f2afe225a40fe7b8e550b95e1ee094c9e8c235cc58258db"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "3b657a135e7d3f1bce6089fabda312fbcfa034bb0f089306e1f4b33839a0f312"
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
