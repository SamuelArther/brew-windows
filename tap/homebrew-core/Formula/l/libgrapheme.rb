class Libgrapheme < Formula
  desc "Unicode string library"
  homepage "https://libs.suckless.org/libgrapheme/"
  url "https://dl.suckless.org/libgrapheme/libgrapheme-3.0.0.tar.gz"
  sha256 "32585af73dda62fbcc0fed14f199aa1bc988ad01dad0bfbd06cf175d9cf3d68c"
  license "ISC"
  head "https://git.suckless.org/libgrapheme/", using: :git, branch: "master"

  livecheck do
    url "https://dl.suckless.org/libgrapheme/"
    regex(/href=.*?libgrapheme[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/libgrapheme-3.0.0"
    sha256 cellar: :any, x86_64_windows: "94259c1ceccc4e69a3cd135fcdb52c9275bc8768bcc2c2ddef00311299be7ade"
  end

  def install
    system "./configure"
    system "make", "PREFIX=#{prefix}", "LDCONFIG=", "install"
  end

  test do
    (testpath/"example.c").write <<~C
      #include <grapheme.h>

      int
      main(void)
      {
        return (grapheme_next_word_break_utf8("Hello World!", SIZE_MAX) != 5);
      }
    C
    system ENV.cc, "example.c", "-I#{include}", "-L#{lib}", "-lgrapheme", "-o", "example"
    system "./example"
  end
end
