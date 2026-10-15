class Cscope < Formula
  desc "Tool for browsing source code"
  homepage "https://cscope.sourceforge.net/"
  url "https://repo.msys2.org/msys/x86_64/cscope-15.9-3-x86_64.pkg.tar.zst"
  sha256 "c42eae84b35132bc8b07211ad22c35400427d83534ca355dc1aef3b81d498be3"
  license "BSD-3-Clause"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://downloads.sourceforge.net/project/cscope/cscope/v15.9/cscope-15.9.tar.gz"
    regex(%r{url=.*?/cscope[._-]v?(\d+(?:\.\d+)+[a-z]?)\.t}i)
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/cscope.exe", "cscope"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <stdio.h>
      #include <stdlib.h>

      void func()
      {
        printf("Hello World!");
      }

      int main()
      {
        func();
        return 0;
      }
    C
    (testpath/"cscope.files").write "./test.c\n"
    system bin/"cscope", "-b", "-k"
    assert_match(/test\.c.*func/, shell_output("#{bin}/cscope -L1func"))
  end
end
