class Moe < Formula
  desc "Console text editor for ISO-8859 and ASCII"
  homepage "https://www.gnu.org/software/moe/moe.html"
  url "https://ftpmirror.gnu.org/moe/moe-1.16.tar.lz"
  mirror "https://ftp.gnu.org/gnu/moe/moe-1.16.tar.lz"
  sha256 "4c25cd78919272aebec0a7f8c126011bb5a4b5d87422807a3423216f0a17a868"
  license "GPL-2.0-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "bee7b2b4cf8fddc379b583d55f531d14f6b0144623609592293722c2be520bd3"
  end

  uses_from_macos "ncurses"

  def install
    system "./configure", "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    system bin/"moe", "--version"
  end
end
