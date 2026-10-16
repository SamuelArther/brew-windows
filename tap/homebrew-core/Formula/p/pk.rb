class Pk < Formula
  desc "Field extractor command-line utility"
  homepage "https://github.com/johnmorrow/pk"
  url "https://github.com/johnmorrow/pk/archive/refs/tags/v2.0.0.tar.gz"
  sha256 "e1d70b683cbf8d1be505e818d91ef07c6938c82affc914eaf88f25d4f81edd56"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "fb60a09ae336110990c45bb54d22f49ca9163761a8a7cac9ab900b89c836dc22"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    # The release does not include a `Cargo.lock`.
    system "cargo", "generate-lockfile"
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
    man1.install "doc/pk.1"
  end

  test do
    assert_equal "B C D", pipe_output("#{bin}/pk 2..4", "A B C D E", 0).chomp
  end
end
