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
    sha256 cellar: :any, x86_64_windows: "ac12e89dea30afb24b130d310477b4987897894bf81678c083609b3718a949d6"
  end

  head do
    url "https://github.com/cacalabs/toilet.git", branch: "master"

    depends_on "autoconf" => :build
    depends_on "automake" => :build
  end

  depends_on "pkgconf" => :build
  depends_on "libcaca"

  def install
    system "./bootstrap" if build.head?
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    system bin/"toilet", "--version"
  end
end
