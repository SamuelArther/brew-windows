class Liblzf < Formula
  desc "Very small, very fast data compression library"
  homepage "https://oldhome.schmorp.de/marc/liblzf.html"
  url "https://dist.schmorp.de/liblzf/liblzf-3.6.tar.gz"
  mirror "https://deb.debian.org/debian/pool/main/libl/liblzf/liblzf_3.6.orig.tar.gz"
  sha256 "9c5de01f7b9ccae40c3f619d26a7abec9986c06c36d260c179cedd04b89fb46a"
  license all_of: [
    "BSD-2-Clause",
    any_of: ["BSD-2-Clause", "GPL-2.0-or-later"], # lzf.c lzf.h lzfP.h lzf_c.c lzf_d.c
  ]

  livecheck do
    url "https://dist.schmorp.de/liblzf/"
    regex(/href=.*?liblzf[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "fb81101c4b44684395cd3b6367461a6fb8fc7fce878ff49364cdd1bad2f90b4c"
  end

  def install
    # MinGW spells the MSVC type __int64 (lzf_c.c's _WIN32 branch says _int64).
    inreplace "lzf_c.c", "unsigned _int64 off", "unsigned __int64 off" if OS.windows?
    system "./configure", "--disable-debug",
                          "--disable-dependency-tracking",
                          "--disable-silent-rules",
                          "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    # Adapted from bench.c in the liblzf source
    (testpath/"test.c").write <<~C
      #include <assert.h>
      #include <string.h>
      #include <stdlib.h>
      #include "lzf.h"
      #define DSIZE 32768
      unsigned char data[DSIZE], data2[DSIZE*2], data3[DSIZE*2];
      int main()
      {
        unsigned int i, l, j;
        for (i = 0; i < DSIZE; ++i)
          data[i] = i + (rand() & 1);
        l = lzf_compress (data, DSIZE, data2, DSIZE*2);
        assert(l);
        j = lzf_decompress (data2, l, data3, DSIZE*2);
        assert (j == DSIZE);
        assert (!memcmp (data, data3, DSIZE));
        return 0;
      }
    C
    system ENV.cc, "test.c", "-L#{lib}", "-llzf", "-o", "test"
    system "./test"
  end
end
