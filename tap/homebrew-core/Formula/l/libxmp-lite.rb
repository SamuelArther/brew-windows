class LibxmpLite < Formula
  desc "Lite libxmp"
  homepage "https://xmp.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/xmp/libxmp/4.7.4/libxmp-lite-4.7.4.tar.gz"
  sha256 "d4b6108b6e8c7ecc106403bc4bab28fb46686867a7b5247567371ad557634c12"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "0d590fe1a453cabf9d503bdb08c26d03008704c7ee0bdb36805ffdf6e38de9dc"
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
