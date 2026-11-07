class LibxmpLite < Formula
  desc "Lite libxmp"
  homepage "https://xmp.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/xmp/libxmp/4.7.4/libxmp-lite-4.7.4.tar.gz"
  sha256 "d4b6108b6e8c7ecc106403bc4bab28fb46686867a7b5247567371ad557634c12"
  license "MIT"

  deny_network_access!

  bottle do
    sha256 cellar: :any, x86_64_windows: "1b206e677e05d3a5104d1037f1ab4948bf1db2ceafe6309dee033138099399b1"
  end

  def install
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <stdio.h>
      #include <libxmp-lite/xmp.h>

      int main(int argc, char* argv[]){
        printf("libxmp-lite %s/%c%u\\n", XMP_VERSION, *xmp_version, xmp_vercode);
        return 0;
      }
    C

    system ENV.cc, "test.c", "-I#{include}", "-L#{lib}", "-lxmp-lite", "-o", "test"
    system "./test"
  end
end
