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
    sha256 cellar: :any, x86_64_windows: "6e6c4c727c78fa20a95c7baf10866971daa8eb7ec3d5448bc4f675acfcd7ece1"
  end

  deny_network_access!

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
