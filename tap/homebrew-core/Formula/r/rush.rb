class Rush < Formula
  desc "GNU's Restricted User SHell"
  homepage "https://www.gnu.org.ua/software/rush/"
  url "https://ftpmirror.gnu.org/rush/rush-2.4.tar.xz"
  mirror "https://ftp.gnu.org/gnu/rush/rush-2.4.tar.xz"
  sha256 "fa95af9d2c7b635581841cc27a1d27af611f60dd962113a93d23a8874aa060f4"
  license "GPL-3.0-or-later"

  conflicts_with "rush-parallel", because: "both install `rush` binaries"

  deny_network_access!

  bottle do
    sha256 cellar: :any, x86_64_windows: "eb8719c84ea3779043bffb672499b031dc24b2bfaa10a1b576cb666ae840ebea"
  end

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
