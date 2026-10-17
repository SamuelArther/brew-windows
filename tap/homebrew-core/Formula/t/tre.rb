class Tre < Formula
  desc "Lightweight, POSIX-compliant regular expression (regex) library"
  homepage "https://github.com/laurikari/tre"
  url "https://github.com/laurikari/tre/releases/download/v0.9.0/tre-0.9.0.tar.gz"
  sha256 "f57f5698cafdfe516d11fb0b71705916fe1162f14b08cf69d7cf86923b5a2477"
  license "BSD-2-Clause"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/tre-0.9.0"
    sha256 cellar: :any, x86_64_windows: "87c67464159a2e6abf51cbbf72e784ddad5c04f7edecfa277bcf843d98b757ba"
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
