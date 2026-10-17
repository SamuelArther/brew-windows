class Libagg < Formula
  desc "High fidelity 2D graphics library for C++"
  homepage "https://agg.sourceforge.net/antigrain.com/"
  # Canonical URL inaccessible: https://antigrain.com/agg-2.5.tar.gz
  url "https://ftp2.osuosl.org/pub/blfs/8.0/a/agg-2.5.tar.gz"
  sha256 "ab1edc54cc32ba51a62ff120d501eecd55fceeedf869b9354e7e13812289911f"
  license "GPL-2.0-or-later"
  revision 1

  # The homepage for this formula is a copy of the original and was created
  # after the original was discontinued. There will be no further releases of
  # the copy of this software used in the formula, as the developer is deceased.
  # New development of libagg occurs in a fork of v2.4 and can be found at:
  # https://sourceforge.net/projects/agg/
  livecheck do
    skip "No longer developed/maintained"
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/libagg-2.5_1"
    sha256 cellar: :any, x86_64_windows: "d3855b7e2190b61d32f74570ced5cb5382a8a909071ae4964aedeb58e75e9b3f"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build
  depends_on "libtool" => :build
  depends_on "pkgconf" => [:build, :test]

  # Apply MacPorts patch to allow building without SDL
  patch :p0 do
    url "https://raw.githubusercontent.com/macports/macports-ports/2c1525dfa5e9b3d60ec0a02cbbd9a5c21a4e05eb/graphics/antigraingeometry/files/patch-configure.in.diff"
    sha256 "c3f22ef7d57cf5f88e4a72fd2ff5c5416610ad9953953fb87f4286bdeee96031"
    type :unofficial
  end
  patch :p0 do
    url "https://raw.githubusercontent.com/macports/macports-ports/2c1525dfa5e9b3d60ec0a02cbbd9a5c21a4e05eb/graphics/antigraingeometry/files/patch-src-platform-Makefile.am.diff"
    sha256 "be4b7a6a118833722f3aaf378d0bcdf4dc56ed8f003edff5bc73fe2b83a49bee"
    type :unofficial
  end

  # Fix build with clang; last release was in 2006
  patch :DATA

  deny_network_access!

  def install
    # AM_C_PROTOTYPES was removed in automake 1.12, as it's only needed for
    # pre-ANSI compilers
    inreplace "configure.in", "AM_C_PROTOTYPES", ""
    inreplace "autogen.sh", "libtoolize", "glibtoolize"

    system "sh", "autogen.sh",
                 "--disable-platform", # Causes undefined symbols
                 "--disable-ctrl",     # No need to run these during configuration
                 "--disable-examples",
                 "--disable-sdltest",
                 *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <cstdio>
      #include <cstring>
      #include "agg_basics.h"
      #include "agg_rendering_buffer.h"
      #include "agg_pixfmt_rgb.h"
      #include "agg_renderer_base.h"
      #include "agg_renderer_scanline.h"
      #include "agg_rasterizer_scanline_aa.h"
      #include "agg_scanline_p.h"
      #include "agg_ellipse.h"
      #include "agg_conv_stroke.h"
      #include "agg_conv_transform.h"
      #include "agg_trans_affine.h"

      int main() {
        const unsigned w = 100, h = 100;
        unsigned char buf[w * h * 3];
        memset(buf, 255, sizeof(buf));

        agg::rendering_buffer rbuf(buf, w, h, w * 3);
        agg::pixfmt_rgb24 pixf(rbuf);
        agg::renderer_base<agg::pixfmt_rgb24> ren(pixf);
        agg::renderer_scanline_aa_solid<agg::renderer_base<agg::pixfmt_rgb24> > sren(ren);
        agg::rasterizer_scanline_aa<> ras;
        agg::scanline_p8 sl;

        agg::ellipse e(0, 0, 10, 10, 64);
        agg::trans_affine mtx = agg::trans_affine_scaling(2.0) * agg::trans_affine_translation(50, 50);
        agg::conv_transform<agg::ellipse> te(e, mtx);
        ras.add_path(te);
        sren.color(agg::rgba8(255, 0, 0));
        agg::render_scanlines(ras, sl, sren);

        agg::ellipse e2(50, 50, 40, 40, 128);
        agg::conv_stroke<agg::ellipse> stroke(e2);
        stroke.width(4.0);
        ras.reset();
        ras.add_path(stroke);
        sren.color(agg::rgba8(0, 0, 255));
        agg::render_scanlines(ras, sl, sren);

        agg::rgba8 c = pixf.pixel(50, 50);
        agg::rgba8 s = pixf.pixel(90, 50);
        agg::rgba8 o = pixf.pixel(2, 2);
        printf("center %d %d %d\\n", c.r, c.g, c.b);
        printf("stroke %d %d %d\\n", s.r, s.g, s.b);
        printf("corner %d %d %d\\n", o.r, o.g, o.b);
        return 0;
      }
    CPP

    flags = shell_output("pkgconf --cflags --libs libagg").chomp.split
    system ENV.cxx, "-std=c++11", "test.cpp", *flags, "-o", "test"
    assert_equal <<~EOS, shell_output("./test")
      center 255 0 0
      stroke 0 0 255
      corner 255 255 255
    EOS
  end
end

__END__
diff --git a/include/agg_renderer_outline_aa.h b/include/agg_renderer_outline_aa.h
index ce25a2e..9a12d35 100644
--- a/include/agg_renderer_outline_aa.h
+++ b/include/agg_renderer_outline_aa.h
@@ -1375,7 +1375,7 @@ namespace agg
         //---------------------------------------------------------------------
         void profile(const line_profile_aa& prof) { m_profile = &prof; }
         const line_profile_aa& profile() const { return *m_profile; }
-        line_profile_aa& profile() { return *m_profile; }
+        const line_profile_aa& profile() { return *m_profile; }

         //---------------------------------------------------------------------
         int subpixel_width() const { return m_profile->subpixel_width(); }
