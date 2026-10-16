class Daktilo < Formula
  desc "Plays typewriter sounds every time you press a key"
  homepage "https://daktilo.cli.rs"
  url "https://github.com/orhun/daktilo/archive/refs/tags/v0.6.0.tar.gz"
  sha256 "594987ad0697a29e3d0dc25b220e5680cfaecedf2175fd6c17ba827fc7bc2978"
  license any_of: ["Apache-2.0", "MIT"]

  bottle do
    sha256 cellar: :any, x86_64_windows: "b4ee61576e5e8fd40c47ac967f6e0a87a042e3264c5573447f56ee5bdb056e72"
  end

  depends_on "pkgconf" => :build
  depends_on "rust" => :build

  on_linux do
    depends_on "alsa-lib"
    depends_on "libx11"
    depends_on "libxi"
    depends_on "libxtst"
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "crates/daktilo")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/daktilo --version")

    output = shell_output("#{bin}/daktilo -l")
    assert_match "kick.mp3,hat.mp3,snare.mp3,kick.mp3,hat.mp3,kick.mp3,snare.mp3,hat.mp3", output
  end
end
