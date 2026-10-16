class Bas55 < Formula
  desc "Minimal BASIC programming language interpreter as defined by ECMA-55"
  homepage "https://jorgicor.niobe.org/bas55/"
  url "https://jorgicor.niobe.org/bas55/bas55-2.0.tar.gz"
  sha256 "640b296df2ee22c2e14e90b23e0edad646682c80b3226652772847fa0614f293"
  license "MIT"

  livecheck do
    url :homepage
    regex(/href=.*?bas55[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e36531f1ef632a0a5bfd88f9f711c0399e840aaf31e2907f698d3b81498a7d9c"
  end

  def install
    system "./configure", "--disable-dependency-tracking",
                          "--disable-silent-rules",
                          "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    (testpath/"hello.bas").write <<~BASIC
      10 PRINT "HELLO, WORLD"
      20 END
    BASIC

    assert_equal "HELLO, WORLD\n", shell_output("#{bin}/bas55 hello.bas")
  end
end
