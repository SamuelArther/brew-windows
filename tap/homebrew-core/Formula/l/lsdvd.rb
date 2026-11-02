class Lsdvd < Formula
  desc "Read the content info of a DVD"
  homepage "https://sourceforge.net/projects/lsdvd/"
  url "https://git.code.sf.net/p/lsdvd/git.git",
      tag:      "0.21",
      revision: "de9cf2379335076368cc848de04a60279d944b68"
  license "GPL-2.0-only"

  bottle do
    sha256 cellar: :any, x86_64_windows: "ee3fa3f8f0a3b6266a8e5217a93f8e865f23a81f3ca1ff3c2180b95cce890ad0"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build
  depends_on "pkgconf" => :build
  depends_on "libdvdcss"
  depends_on "libdvdread"
  depends_on "libxml2"

  # Move `dvdlogger` function out of `main()`, as Clang (rightfully) does not allow nested functions
  # Can be removed once this has been merged: https://sourceforge.net/p/lsdvd/git/merge-requests/2/
  patch do
    file "Patches/lsdvd/logging.patch"
  end

  deny_network_access!

  def install
    system "autoreconf", "--force", "--install", "--verbose"
    system "./configure", "--mandir=#{man}", *std_configure_args
    system "make", "install"
  end

  test do
    system bin/"lsdvd", "--help"
  end
end
