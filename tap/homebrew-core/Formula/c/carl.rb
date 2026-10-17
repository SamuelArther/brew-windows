class Carl < Formula
  desc "Calendar for the command-line"
  homepage "https://codeberg.org/birger/carl"
  # Missing codeberg tag, use crate url instead
  url "https://static.crates.io/crates/carl/carl-0.6.0.crate"
  sha256 "225a66d6b91fc6fe15ff1a7afc9f8c1d461f2885aa9a115e06947d3d81f20da3"
  license "MIT"
  head "https://codeberg.org/birger/carl.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "23af6cbc61c63d45f44fd4ee27d24dd4f3bea3918ec0ba12fe991de5381c2062"
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
    assert_match "Su Mo Tu We Th Fr Sa", shell_output("#{bin}/carl --sunday")
    assert_match "Mo Tu We Th Fr Sa Su", shell_output("#{bin}/carl --monday")

    output = shell_output("#{bin}/carl --year")
    %w[
      January February March April May June July
      August September October November December
    ].each do |month|
      assert_match month, output
    end
  end
end
