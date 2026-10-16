class RunKit < Formula
  desc "Universal multi-language runner and smart REPL"
  homepage "https://github.com/Esubaalew/run"
  url "https://github.com/Esubaalew/run/archive/refs/tags/v0.10.0.tar.gz"
  sha256 "fcb22f803107cc7a7a5a3bcafc37e12e150d50ea6b41837577a750779774c1a2"
  license "Apache-2.0"
  head "https://github.com/Esubaalew/run.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "23d4b049a547df5b43d14ce02a0fcb2a04ca9e070f4231ab4b81b9a83c117e2d"
  end

  depends_on "rust" => :build

  conflicts_with "run", because: "both install a `run` binary"

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    assert_match "brew", shell_output("#{bin}/run bash \"echo brew\"")
  end
end
