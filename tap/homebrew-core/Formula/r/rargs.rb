class Rargs < Formula
  desc "Util like xargs + awk with pattern matching support"
  homepage "https://github.com/lotabout/rargs"
  url "https://github.com/lotabout/rargs/archive/refs/tags/v0.3.0.tar.gz"
  sha256 "22d9aa4368a0f9d1fd82391439d3aabf4ddfb24ad674a680d6407c9e22969da3"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "d436645fa661b153c4fba06f5d7ed64eb758b4c10501aee1edfd5ca513280314"
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
    assert_equal "abc", pipe_output("#{bin}/rargs -d, echo {1}", "abc,def").chomp
  end
end
