class Dnsmap < Formula
  desc "Passive DNS network mapper (a.k.a. subdomains bruteforcer)"
  homepage "https://github.com/resurrecting-open-source-projects/dnsmap"
  url "https://github.com/resurrecting-open-source-projects/dnsmap/archive/refs/tags/0.36.tar.gz"
  sha256 "f52d6d49cbf9a60f601c919f99457f108d51ecd011c63e669d58f38d50ad853c"
  # Code is all GPL-2.0-or-later but license file was changed to GPL-3.0 in following commit
  # Ref: https://github.com/resurrecting-open-source-projects/dnsmap/commit/408ecfd62a0b2c089dda6f3be5d396ed2662797e
  license all_of: ["GPL-2.0-or-later", "GPL-3.0-or-later"]
  head "https://github.com/resurrecting-open-source-projects/dnsmap.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/dnsmap-0.36"
    sha256 cellar: :any, x86_64_windows: "27cbab60d9c66a9c26ba82f5a8ff910541c5b4d8ec9c05d76283c6b92d311f33"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build

  def install
    system "./autogen.sh"
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    assert_match version.to_s, shell_output(bin/"dnsmap", 1)
  end
end
