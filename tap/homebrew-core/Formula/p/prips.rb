class Prips < Formula
  desc "Print the IP addresses in a given range"
  homepage "https://devel.ringlet.net/sysutils/prips/"
  url "https://devel.ringlet.net/files/sys/prips/prips-1.3.1.tar.xz"
  sha256 "5369056ec32216ec4aabf93bc410a1b8a40f04003ec923fc0250a4427bfe009d"
  license "GPL-2.0-or-later"

  livecheck do
    url "https://devel.ringlet.net/sysutils/prips/download/"
    regex(/href=.*?prips[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "04a170f371effeff9a1751f1e6c37bde4087fc5b6def697f0eeb9e498a8a439e"
  end

  depends_on "rust" => :build

  def install
    system "cargo", "install", *std_cargo_args(path: "rust")
    man1.install "prips.1"
  end

  test do
    assert_equal "127.0.0.0\n127.0.0.1",
      shell_output("#{bin}/prips 127.0.0.0/31").strip
  end
end
