class Ratfor < Formula
  desc "Rational Fortran"
  homepage "http://www.dgate.org/ratfor/"
  # Upstream is only available via HTTP, so we prefer Debian's HTTPS mirror
  url "https://deb.debian.org/debian/pool/main/r/ratfor/ratfor_1.07.orig.tar.gz"
  mirror "http://www.dgate.org/ratfor/tars/ratfor-1.07.tar.gz"
  sha256 "943b5de328d7b890cb444b17fb7dab656ffaa0d388c7d40b649d34b736b137ff"
  license :public_domain

  livecheck do
    url :homepage
    regex(/href=.*?ratfor[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "5e7b8e19ad152dfa4ea7264b8cc9605159ce84803b3cbaf84a304ff40c16032e"
  end

  depends_on "gcc" # for gfortran

  def install
    system "./configure", "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    (testpath/"test.r").write <<~FORTRAN
      integer x,y
      x=1; y=2
      if(x == y)
              write(6,600)
      else if(x > y)
              write(6,601)
      else
              write(6,602)
      x=1
      while(x < 10){
        if(y != 2) break
        if(y != 2) next
        write(6,603)x
        x=x+1
        }
      repeat
        x=x-1
      until(x == 0)
      for(x=0; x < 10; x=x+1)
              write(6,604)x
      600 format('Wrong, x != y')
      601 format('Also wrong, x < y')
      602 format('Ok!')
      603 format('x = ',i2)
      604 format('x = ',i2)
      end
    FORTRAN

    system bin/"ratfor", "-o", "test.f", testpath/"test.r"
    system "gfortran", "test.f", "-o", "test"
    system "./test"
  end
end
