class Monocle < Formula
  desc "See through all BGP data with a monocle"
  homepage "https://github.com/bgpkit/monocle"
  url "https://github.com/bgpkit/monocle/archive/refs/tags/v1.6.1.tar.gz"
  sha256 "4aff8f4d13b300f87d6ef49dbb417dc2cbcdbbd3cc64babb0affdc2c3d663aa3"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "083ae944b1f21189911c9bf9faa9e1df97b9c6578ed3c1cd77dbd09f7079a20f"
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
    output = shell_output("#{bin}/monocle time 1735322400 --simple")
    assert_match "2024-12-27T18:00:00+00:00", output
  end
end
