class RpgCli < Formula
  desc "Your filesystem as a dungeon!"
  homepage "https://olano.dev/blog/deconstructing-the-role-playing-videogame/"
  url "https://github.com/facundoolano/rpg-cli/archive/refs/tags/1.2.0.tar.gz"
  sha256 "f3993abe7b73666bc3707760dcc650aa9190cd3e7f06be846a0b6adcbbc46663"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a8d040f3818675bef07a4e8a3333ba30ed55b83696c668c1e24b2f0930d47e98"
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
    output = shell_output(bin/"rpg-cli").strip
    assert_match "hp", output
    assert_match "equip", output
    assert_match "item", output
  end
end
