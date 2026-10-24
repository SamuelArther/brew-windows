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
    sha256 cellar: :any, x86_64_windows: "a21d2bb020467cf11a931e427e63213d18f15db4ad7ac5012ca4da9b59c8dedd"
  end

  depends_on "rust" => :build

  allow_network_access! :test

  def fetch
    cd "rust" do
      system "cargo", "fetch", *std_cargo_fetch_args
    end
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "rust")
    man1.install "prips.1"
  end

  test do
    assert_equal "127.0.0.0\n127.0.0.1",
      shell_output("#{bin}/prips 127.0.0.0/31").strip
  end
end
