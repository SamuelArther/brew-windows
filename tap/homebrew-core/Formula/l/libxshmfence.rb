class Libxshmfence < Formula
  desc "X.Org: Shared memory 'SyncFence' synchronization primitive"
  homepage "https://www.x.org/"
  url "https://xorg.freedesktop.org/archive/individual/lib/libxshmfence-1.3.3.tar.xz"
  sha256 "d4a4df096aba96fea02c029ee3a44e11a47eb7f7213c1a729be83e85ec3fde10"
  license "MIT"
  compatibility_version 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "197fc9b9c22dad1ee95a30a597b9f433eb03417c5441fcbce752d779c7807c6e"
  end

  depends_on "pkgconf" => :build
  depends_on "xorgproto" => [:build, :test]

  deny_network_access!

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
  end
end
