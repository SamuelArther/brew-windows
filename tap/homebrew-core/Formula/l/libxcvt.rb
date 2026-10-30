class Libxcvt < Formula
  desc "VESA CVT standard timing modelines generator"
  homepage "https://www.x.org"
  url "https://www.x.org/releases/individual/lib/libxcvt-0.1.3.tar.xz"
  sha256 "a929998a8767de7dfa36d6da4751cdbeef34ed630714f2f4a767b351f2442e01"
  license "MIT"
  compatibility_version 1
  head "https://gitlab.freedesktop.org/xorg/lib/libxcvt.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "01c01287e18e7abfa6f09f099e81ed3fec0fa6b34051056de995bd88882f725e"
  end

  depends_on "meson" => :build
  depends_on "ninja" => :build

  def install
    system "meson", "setup", "build", *std_meson_args
    system "meson", "compile", "-C", "build", "--verbose"
    system "meson", "install", "-C", "build"
  end

  test do
    assert_match "1920", shell_output("#{bin}/cvt 1920 1200 75")

    (testpath/"test.c").write <<~C
      #include <libxcvt/libxcvt.h>
      #include <assert.h>
      #include <stdio.h>

      int main(void) {
        struct libxcvt_mode_info *pmi = libxcvt_gen_mode_info(1920, 1200, 75, false, false);
        assert(pmi->hdisplay == 1920);
        return 0;
      }
    C
    system ENV.cc, "./test.c", "-o", "test", "-I#{include}", "-L#{lib}", "-lxcvt"
    system "./test"
  end
end
