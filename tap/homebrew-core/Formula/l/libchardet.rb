class Libchardet < Formula
  desc "Mozilla's Universal Charset Detector C/C++ API"
  homepage "https://github.com/Joungkyun/libchardet"
  url "https://github.com/Joungkyun/libchardet/releases/download/1.0.6/libchardet-1.0.6.tar.bz2"
  sha256 "8958d5738c8f7af351cc8f718e922c7961fd2f095fc668bff59ae2a531231592"
  license any_of: ["MPL-1.1", "LGPL-2.1-only"]

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/libchardet-1.0.6"
    sha256 cellar: :any, x86_64_windows: "c98dcb2759d4fc8487cc98628b32488dd110c2599276249d61bd77cd00cba954"
  end

  depends_on "pkgconf" => :test

  # Fix -flat_namespace being used on Big Sur and later.
  patch do
    file "Patches/libtool/configure-pre-0.4.2.418-big_sur.diff"
    type :unofficial
  end

  def install
    system "./configure", "--disable-silent-rules", *std_configure_args
    system "make"
    system "make", "install"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <assert.h>
      #include <chardet.h>
      #include <string.h>

      int main(void) {
        DetectObj *o;
        char *str = "안녕하세요";

        if ((o = detect_obj_init()) == NULL)
          return 1;

        switch (detect_r(str, strlen(str), &o)) {
          case CHARDET_NULL_OBJECT:
          case CHARDET_OUT_OF_MEMORY:
            return 1;

          default:
            assert(strcmp(o->encoding, "UTF-8") == 0);
            return 0;
        }
      }
    C
    flags = shell_output("#{formula_opt_bin("pkgconf")}/pkgconf --cflags --libs chardet").strip.split
    system ENV.cc, "test.c", "-o", "test", *flags
    system "./test"

    assert_match version.to_s, (lib/"pkgconfig/chardet.pc").read
  end
end
