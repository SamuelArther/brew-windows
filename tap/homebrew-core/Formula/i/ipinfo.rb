class Ipinfo < Formula
  desc "Tool for calculation of IP networks"
  homepage "https://kyberdigi.cz/projects/ipinfo/"
  url "https://kyberdigi.cz/projects/ipinfo/files/ipinfo-1.2.tar.gz"
  sha256 "19e6659f781a48b56062a5527ff463a29c4dcc37624fab912d1dce037b1ddf2d"
  license "Beerware"

  # The content of the download page is generated using JavaScript and software
  # versions are the first string in certain array literals in the page source.
  livecheck do
    url :homepage
    regex(/(?:new Array\(|\[)["']v?(\d+(?:\.\d+)+)["'],/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "161a0a60e01fc4fcc95d21f8b7269c247297e751d9f1235f03d2d32d71e4bf33"
  end

  conflicts_with "ipinfo-cli", because: "ipinfo and ipinfo-cli install the same binaries"

  def install
    system "make", "BINDIR=#{bin}", "MANDIR=#{man1}", "install"
  end

  test do
    system bin/"ipinfo", "127.0.0.1"
  end
end
