class Libxshmfence < Formula
  desc "X.Org: Shared memory 'SyncFence' synchronization primitive"
  homepage "https://www.x.org/"
  url "https://xorg.freedesktop.org/archive/individual/lib/libxshmfence-1.3.3.tar.xz"
  sha256 "d4a4df096aba96fea02c029ee3a44e11a47eb7f7213c1a729be83e85ec3fde10"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "fe69eca9146c2eb22c441f6d22d8fc2f227b1ab98fc5a2d5bb4a5b7dda101a16"
  end

  depends_on "pkgconf" => :build
  depends_on "xorgproto" => [:build, :test]

  def install
    args = %W[
      --sysconfdir=#{etc}
      --localstatedir=#{var}
      --disable-silent-rules
    ]

    system "./configure", *args, *std_configure_args
    system "make"
    system "make", "install"
  end

  test do
    (testpath/"test.c").write <<~C
      #include "X11/xshmfence.h"

      int main(int argc, char* argv[]) {
        struct xshmfence *fence;
        return 0;
      }
    C
    system ENV.cc, "test.c"
    assert_equal 0, $CHILD_STATUS.exitstatus
  end
end
