class Libmxml < Formula
  desc "Mini-XML library"
  homepage "https://michaelrsweet.github.io/mxml/"
  url "https://github.com/michaelrsweet/mxml/releases/download/v4.0.6/mxml-4.0.6.tar.gz"
  sha256 "ec1af6f7a752f63649ad00cf7355cf36366ccf729658a1a4696270de8bd74854"
  license "Apache-2.0"
  head "https://github.com/michaelrsweet/mxml.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "42b2c0885046526103b37301f95427b6fa0028e6cf3eb1c1d65701c586ab9d6c"
  end

  depends_on "pkgconf" => :test

  deny_network_access!

  def install
    system "./configure", "--enable-shared", *std_configure_args
    system "make"
    system "make", "install"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <mxml.h>

      int main()
      {
        FILE *fp;
        mxml_node_t *tree;

        fp = fopen("test.xml", "r");
        tree = mxmlLoadFile(NULL, NULL, fp);
        fclose(fp);
      }
    C

    (testpath/"test.xml").write <<~XML
      <?xml version="1.0" encoding="UTF-8"?>
      <test>
        <text>I'm an XML document.</text>
      </test>
    XML

    flags = shell_output("pkgconf --cflags --libs mxml4").chomp.split
    system ENV.cc, "test.c", "-o", "test", *flags
    system "./test"
  end
end
