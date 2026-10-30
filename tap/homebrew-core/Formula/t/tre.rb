class Tre < Formula
  desc "Lightweight, POSIX-compliant regular expression (regex) library"
  homepage "https://github.com/laurikari/tre"
  url "https://github.com/laurikari/tre/releases/download/v0.9.0/tre-0.9.0.tar.gz"
  sha256 "f57f5698cafdfe516d11fb0b71705916fe1162f14b08cf69d7cf86923b5a2477"
  license "BSD-2-Clause"
  compatibility_version 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "075d10436a79afaced7c624fd1edd2f1ecce1f767929da1fb3bf6f737e6478f0"
  end

  def install
    system "./configure", "--disable-debug",
                          "--disable-dependency-tracking",
                          "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    assert_equal "brow", pipe_output("#{bin}/agrep -1 brew", "brow", 0)
  end
end
