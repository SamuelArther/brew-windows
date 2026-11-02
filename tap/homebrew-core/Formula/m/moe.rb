class Moe < Formula
  desc "Console text editor for ISO-8859 and ASCII"
  homepage "https://www.gnu.org/software/moe/moe.html"
  url "https://ftpmirror.gnu.org/moe/moe-1.16.tar.lz"
  mirror "https://ftp.gnu.org/gnu/moe/moe-1.16.tar.lz"
  sha256 "4c25cd78919272aebec0a7f8c126011bb5a4b5d87422807a3423216f0a17a868"
  license "GPL-2.0-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "216ca68afacc11ed4c2251f222f7e5c7e717d02e24cd5f444887776170696c3d"
  end

  uses_from_macos "ncurses"

  deny_network_access!

  def install
    system "./configure", "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    system bin/"moe", "--version"
  end
end
