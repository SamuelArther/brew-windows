class Qstat < Formula
  desc "Query Quake servers from the command-line"
  homepage "https://github.com/rocketsciencegg/qstat"
  url "https://github.com/rocketsciencegg/qstat/archive/refs/tags/v2.18.tar.gz"
  sha256 "a74564bd9c31db3dc1fcc0a68ffaf694630b9e67f0d31ff76b2a3c3196ee4f1f"
  license "Artistic-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "7b1954a8c9f21f125c688dbd4721eb6ef62af4d0091cbf7423ece903a75ec49a"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build

  deny_network_access!

  def install
    system "autoupdate" unless OS.mac?
    system "./autogen.sh"
    system "./configure", "--disable-debug",
                          "--disable-dependency-tracking",
                          "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    system bin/"qstat", "--help"
  end
end
