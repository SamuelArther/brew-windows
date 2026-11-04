class Toilet < Formula
  desc "Color-based alternative to figlet (uses libcaca)"
  homepage "http://caca.zoy.org/wiki/toilet"
  url "https://deb.debian.org/debian/pool/main/t/toilet/toilet_0.3.orig.tar.gz"
  mirror "http://caca.zoy.org/raw-attachment/wiki/toilet/toilet-0.3.tar.gz"
  sha256 "89d4b530c394313cc3f3a4e07a7394fa82a6091f44df44dfcd0ebcb3300a81de"
  license "WTFPL"

  livecheck do
    url "http://caca.zoy.org/raw-attachment/wiki/toilet/"
    regex(/href=.*?toilet[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "fa130f4b141ff57d9ea9421ed2911f567089ef4733a350173057401b8709241f"
  end

  head do
    url "https://github.com/cacalabs/toilet.git", branch: "master"

    depends_on "autoconf" => :build
    depends_on "automake" => :build
  end

  depends_on "pkgconf" => :build
  depends_on "libcaca"

  deny_network_access!

  def install
    system "./bootstrap" if build.head?
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    system bin/"toilet", "--version"
  end
end
