class Ekhtml < Formula
  desc "Forgiving SAX-style HTML parser"
  homepage "https://ekhtml.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/ekhtml/ekhtml/0.3.2/ekhtml-0.3.2.tar.gz"
  sha256 "1ed1f0166cd56552253cd67abcfa51728ff6b88f39bab742dbf894b2974dc8d6"
  license "BSD-2-Clause"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/ekhtml-0.3.2"
    sha256 cellar: :any, x86_64_windows: "93bbb3015a4479227f91c862527a1109e844a6b2ef5972592f1204028b963218"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build
  depends_on "libtool" => :build

  deny_network_access!

  def install
    ENV.deparallelize
    # Run autoreconf on macOS to rebuild configure script so that it doesn't try
    # to build with a flat namespace and on Linux to help detect arm64 linux
    system "autoreconf", "--force", "--verbose", "--install"
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <stdio.h>
      #include <string.h>
      #include <ekhtml.h>

      static void on_start(void *cbdata, ekhtml_string_t *tag, ekhtml_attr_t *attrs) {
        printf("start %.*s", (int)tag->len, tag->str);
        for (ekhtml_attr_t *a = attrs; a; a = a->next)
          printf(" %.*s=%.*s", (int)a->name.len, a->name.str, (int)a->val.len, a->val.str);
        printf("\\n");
      }

      static void on_end(void *cbdata, ekhtml_string_t *tag) {
        printf("end %.*s\\n", (int)tag->len, tag->str);
      }

      static void on_data(void *cbdata, ekhtml_string_t *data) {
        if (data->len) printf("data %.*s\\n", (int)data->len, data->str);
      }

      int main(void) {
        const char *html = "<p><a href=\\"https://brew.sh\\">Homebrew</a></p>";
        ekhtml_string_t input = { html, strlen(html) };
        ekhtml_parser_t *parser = ekhtml_parser_new(NULL);

        ekhtml_parser_startcb_add(parser, NULL, on_start);
        ekhtml_parser_endcb_add(parser, NULL, on_end);
        ekhtml_parser_datacb_set(parser, on_data);
        ekhtml_parser_feed(parser, &input);
        ekhtml_parser_flush(parser, 1);
        ekhtml_parser_destroy(parser);
        return 0;
      }
    C

    system ENV.cc, "test.c", "-I#{include}", "-L#{lib}", "-lekhtml", "-o", "test"
    assert_match "start A href=https://brew.sh", shell_output("./test")
  end
end
