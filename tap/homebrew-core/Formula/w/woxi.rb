class Woxi < Formula
  desc "Interpreter for a subset of the Wolfram Language"
  homepage "https://github.com/ad-si/Woxi"
  url "https://github.com/ad-si/Woxi/archive/refs/tags/v0.3.0.tar.gz"
  sha256 "227a3712067071454658802d724ace2bee92572d9a371d456f51c480df9e93b6"
  license "AGPL-3.0-or-later"
  head "https://github.com/ad-si/Woxi.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a78fd88ffb7fb80061ba41881264cbc36550c090bd5f04ad1c57469bc2704cec"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", "--bin", "woxi", *std_cargo_args
  end

  test do
    assert_equal "3", shell_output("#{bin}/woxi eval 'Plus[1, 2]'").strip
    assert_match version.to_s, shell_output("#{bin}/woxi --version")
  end
end
