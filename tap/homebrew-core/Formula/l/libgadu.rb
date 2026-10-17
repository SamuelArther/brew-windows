class Libgadu < Formula
  desc "Library for ICQ instant messenger protocol"
  homepage "https://libgadu.net/"
  url "https://github.com/wojtekka/libgadu/releases/download/1.12.2/libgadu-1.12.2.tar.gz"
  sha256 "28e70fb3d56ed01c01eb3a4c099cc84315d2255869ecf08e9af32c41d4cbbf5d"
  license "LGPL-2.1-only"

  deprecate! date: "2026-02-20", because: :repo_archived
  disable! date: "2027-02-20", because: :repo_archived

  bottle do
    sha256 cellar: :any, x86_64_windows: "5e39def485139e80c1b6a6ae0bc253b67e0c990315fd0db9bd47675c2e1da8ec"
  end

  depends_on "pkgconf" => :test

  on_linux do
    depends_on "zlib-ng-compat"
  end

  # Fix -flat_namespace being used on Big Sur and later.
  patch do
    file "Patches/libtool/configure-pre-0.4.2.418-big_sur.diff"
  end

  deny_network_access!

  def install
    system "./configure", "--without-pthread", *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <stdio.h>
      #include <libgadu.h>

      int main(void) {
        printf("%08x\\n", gg_crc32(0, (const unsigned char *)"123456789", 9));
        return 0;
      }
    C

    pkgconf_flags = shell_output("pkgconf --cflags --libs libgadu").chomp.split
    system ENV.cc, "test.c", "-DGG_IGNORE_DEPRECATED", "-o", "test", *pkgconf_flags
    assert_equal "cbf43926", shell_output("./test").strip
  end
end
