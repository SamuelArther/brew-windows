class Qstat < Formula
  desc "Query Quake servers from the command-line"
  homepage "https://github.com/rocketsciencegg/qstat"
  url "https://github.com/rocketsciencegg/qstat/archive/refs/tags/v2.18.tar.gz"
  sha256 "a74564bd9c31db3dc1fcc0a68ffaf694630b9e67f0d31ff76b2a3c3196ee4f1f"
  license "Artistic-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "447e136bee053194fda949c6e78660bab54c1b66d37af4520e1675d0208088e1"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build

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
