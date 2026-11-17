class Prips < Formula
  desc "Print the IP addresses in a given range"
  homepage "https://devel.ringlet.net/sysutils/prips/"
  url "https://devel.ringlet.net/files/sys/prips/prips-1.4.0.tar.xz"
  sha256 "f2278ccd3dd3656a4f3ed5bd983b04719c24880ab2b87972d972f005bc138943"
  license "GPL-2.0-or-later"

  livecheck do
    url "https://devel.ringlet.net/sysutils/prips/download/"
    regex(/href=.*?prips[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "e7a1eb3d97cc9be4b85b29bfca1c3982abfc9434838a58cd417d0d4499795f5c"
  end

  depends_on "rust" => :build

  allow_network_access! :test

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "rust/crates/prips")
    man1.install "prips.1"
  end

  test do
    assert_equal "127.0.0.0\n127.0.0.1",
      shell_output("#{bin}/prips 127.0.0.0/31").strip
  end
end
