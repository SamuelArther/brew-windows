class Xtrans < Formula
  desc "X.Org: X Network Transport layer shared code"
  homepage "https://www.x.org/"
  url "https://xorg.freedesktop.org/archive/individual/lib/xtrans-1.6.0.tar.xz"
  sha256 "faafea166bf2451a173d9d593352940ec6404145c5d1da5c213423ce4d359e92"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "726f7d1bce227b23e7a8e1fd392370fc12e96fd2f54ebfd565df81b54fa0850d"
  end

  depends_on "pkgconf" => :build
  depends_on "util-macros" => :build
  depends_on "xorgproto" => :test

  def install
    args = %W[
      --sysconfdir=#{etc}
      --localstatedir=#{var}
      --disable-silent-rules
      --enable-docs=no
    ]

    system "./configure", *args, *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"test.c").write <<~C
      #include "X11/Xtrans/Xtrans.h"

      int main(int argc, char* argv[]) {
        Xtransaddr addr;
        return 0;
      }
    C
    system ENV.cc, "test.c"
    assert_equal 0, $CHILD_STATUS.exitstatus
  end
end
