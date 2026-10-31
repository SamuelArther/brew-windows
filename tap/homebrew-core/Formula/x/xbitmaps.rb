class Xbitmaps < Formula
  desc "Bitmap images used by multiple X11 applications"
  homepage "https://xcb.freedesktop.org"
  url "https://xorg.freedesktop.org/archive/individual/data/xbitmaps-1.1.4.tar.xz"
  sha256 "895722f136e21e728c52f2d99fd2dae95018b9ddad1bac1f29d61bcd6593721d"
  license "MIT"
  compatibility_version 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "367653304de35fe2c40a41ef599598067ea650cbfc3e90248aba8503691c8ccc"
  end

  depends_on "pkgconf" => :build
  depends_on "util-macros" => :build

  deny_network_access!

  def install
    args = %W[
      --sysconfdir=#{etc}
      --localstatedir=#{var}
      --disable-silent-rules
    ]

    system "./configure", *args, *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"test.c").write <<~'C'
      #include <X11/bitmaps/gray>
      #include <stdio.h>
      int main() {
        printf("gray_width = %d\n", gray_width);
        return 0;
      }
    C
    system ENV.cc, "test.c", "-o", "test", "-I#{include}"
    assert_equal "gray_width = 2", shell_output("./test").strip
  end
end
