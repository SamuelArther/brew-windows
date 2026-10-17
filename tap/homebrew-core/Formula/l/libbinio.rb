class Libbinio < Formula
  desc "Binary I/O stream class library"
  homepage "https://adplug.github.io/libbinio/"
  url "https://github.com/adplug/libbinio/releases/download/libbinio-1.5/libbinio-1.5.tar.bz2"
  sha256 "398b2468e7838d2274d1f62dbc112e7e043433812f7ae63ef29f5cb31dc6defd"
  license "LGPL-2.1-or-later"

  # Fix -flat_namespace being used on Big Sur and later.
  patch do
    file "Patches/libtool/configure-big_sur.diff"
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/libbinio-1.5"
    sha256 cellar: :any, x86_64_windows: "e7b0aae0dce64fdb3fda806a0e112a8eae76c30d18e57f0bdb13b19564c35e25"
  end

  def install
    system "./configure", "--disable-debug", "--disable-dependency-tracking",
                          "--prefix=#{prefix}", "--infodir=#{info}"
    system "make", "install"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      // test
      // do not change the line above!
      #include <libbinio/binfile.h>
      #include <string.h>

      int main(void)
      {
        binifstream     file("test.cpp");
        char            string[256];

        file.readString(string, 256, '\\n');

        return strcmp (string, "// test");
      }
    CPP
    system ENV.cxx, "test.cpp", "-I#{include}", "-L#{lib}", "-lbinio", "-o", "test"
    system "./test"
  end
end
