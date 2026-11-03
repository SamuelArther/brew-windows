class Libgfshare < Formula
  desc "Library for sharing secrets"
  homepage "https://github.com/kinnison/libgfshare"
  url "https://github.com/kinnison/libgfshare/archive/refs/tags/2.0.0.tar.gz"
  sha256 "91d7ea7f3e5ddb3854a38827a3f6ea7c597db03067735dc953bd31c5b90f9930"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "4c4d96de50efc200958fd3ee7cd44e430f8c8c643e259b3fa1968cfa4911acd9"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build
  depends_on "libtool" => :build

  deny_network_access!

  def install
    system "autoreconf", "--force", "--install", "--verbose"
    system "./configure", "--disable-debug",
                          "--disable-dependency-tracking",
                          "--disable-silent-rules",
                          "--disable-linker-optimisations",
                          "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    touch "test.in"
    system bin/"gfsplit", "test.in"
    system bin/"gfcombine test.in.*"
  end
end
