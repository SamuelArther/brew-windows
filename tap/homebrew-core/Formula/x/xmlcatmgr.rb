class Xmlcatmgr < Formula
  desc "Manipulate SGML and XML catalogs"
  homepage "https://xmlcatmgr.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/xmlcatmgr/xmlcatmgr/2.2/xmlcatmgr-2.2.tar.gz"
  sha256 "ea1142b6aef40fbd624fc3e2130cf10cf081b5fa88e5229c92b8f515779d6fdc"
  license "BSD-3-Clause"

  bottle do
    sha256 cellar: :any, x86_64_windows: "cb05151331ce8e42a5c7a9de6ac6545b1a0482ad20afac0340f764104fab8996"
  end

  def install
    system "./configure", "--disable-debug",
                          "--disable-dependency-tracking",
                          "--prefix=#{prefix}",
                          "--mandir=#{man}"
    system "make", "install"
  end

  test do
    system bin/"xmlcatmgr", "-v"
  end
end
