class Rush < Formula
  desc "GNU's Restricted User SHell"
  homepage "https://www.gnu.org.ua/software/rush/"
  url "https://ftpmirror.gnu.org/rush/rush-2.4.tar.xz"
  mirror "https://ftp.gnu.org/gnu/rush/rush-2.4.tar.xz"
  sha256 "fa95af9d2c7b635581841cc27a1d27af611f60dd962113a93d23a8874aa060f4"
  license "GPL-3.0-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "9d67309cd3630417571371d5bee6997b1f6e3aa3c9e9c9eb329fb74b56e0e599"
  end

  conflicts_with "rush-parallel", because: "both install `rush` binaries"

  def install
    system "./configure", "--disable-debug",
                          "--disable-dependency-tracking",
                          "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    system sbin/"rush", "-h"
  end
end
