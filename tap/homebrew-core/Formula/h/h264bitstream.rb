class H264bitstream < Formula
  desc "Library for reading and writing H264 video streams"
  homepage "https://h264bitstream.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/h264bitstream/h264bitstream/0.2.0/h264bitstream-0.2.0.tar.gz"
  sha256 "94912cb07ef67da762be9c580b325fd8957ad400793c9030f3fb6565c6d263a7"
  license "LGPL-2.1-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "9dc4e9fc0d67bb214941c43be506dc3b63c6d9e6e0d8cd99836b1e527f831b61"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build
  depends_on "libtool" => :build
  depends_on "pkgconf" => :test

  deny_network_access!

  def install
    system "autoreconf", "--force", "--install", "--verbose"
    system "./configure", *std_configure_args
    system "make", "install"
    inreplace lib/"pkgconfig/libh264bitstream.pc", "${libdir}/libh264bitstream.la", "-lh264bitstream"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <stdio.h>
      #include <stdint.h>
      #include <h264bitstream/h264_stream.h>

      int main(void) {
        uint8_t buf[] = {
          0x00, 0x00, 0x00, 0x01, 0x67, 0x64, 0x00, 0x1e, 0xac, 0xd9, 0x40, 0xa0,
          0x3d, 0xb0, 0x11, 0x00, 0x00, 0x03, 0x00, 0x01, 0x00, 0x00, 0x03, 0x00,
          0x32, 0x8f, 0x16, 0x2d, 0x96, 0x00, 0x00, 0x00, 0x01
        };
        int nal_start, nal_end;
        h264_stream_t* h = h264_new();
        if (find_nal_unit(buf, sizeof(buf), &nal_start, &nal_end) <= 0) return 1;
        read_nal_unit(h, &buf[nal_start], nal_end - nal_start);
        printf("type=%d profile=%d level=%d %dx%d\\n", h->nal->nal_unit_type,
               h->sps->profile_idc, h->sps->level_idc,
               (h->sps->pic_width_in_mbs_minus1 + 1) * 16,
               (h->sps->pic_height_in_map_units_minus1 + 1) * 16);
        h264_free(h);
        return 0;
      }
    C
    flags = shell_output("pkg-config --cflags --libs libh264bitstream").chomp.split
    system ENV.cc, "test.c", *flags, "-o", "test"
    assert_equal "type=7 profile=100 level=30 640x480", shell_output("./test").strip
  end
end
