class Srgn < Formula
  desc "Code surgeon for precise text and code transplantation"
  homepage "https://github.com/alexpovel/srgn"
  url "https://github.com/alexpovel/srgn/archive/refs/tags/srgn-v0.14.2.tar.gz"
  sha256 "2f39cbb6e86e3bfe0a01d7727b6d287a2c2399a9ceeda7ee2f47e7c00503b194"
  license "MIT"
  head "https://github.com/alexpovel/srgn.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "978652866e6bf56b9ab59baa2460e968e46b2e2cd7eb22a4c795857241367e59"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args

    generate_completions_from_executable(bin/"srgn", "--completions")
  end

  test do
    assert_match "H____", pipe_output("#{bin}/srgn '[a-z]' -- '_'", "Hello")

    test_string = "Hide ghp_th15 and ghp_th4t"
    assert_match "Hide * and *", pipe_output("#{bin}/srgn '(ghp_[[:alnum:]]+)' -- '*'", test_string)

    assert_match version.to_s, shell_output("#{bin}/srgn --version")
  end
end
