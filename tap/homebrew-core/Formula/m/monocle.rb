class Monocle < Formula
  desc "See through all BGP data with a monocle"
  homepage "https://github.com/bgpkit/monocle"
  url "https://github.com/bgpkit/monocle/archive/refs/tags/v1.6.0.tar.gz"
  sha256 "1940d5c880a18e1839d327a87abdf29c3ec5ab95c45fcfd76b71f60ae2274ec5"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "093c252425051b5ccf39362e157d18417712542bc3a0d428f38122d13619dfbd"
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
