class Libelf < Formula
  desc "ELF object file access library"
  # Original upstream is gone https://web.archive.org/web/20181111033959/www.mr511.de/software/english.html
  homepage "https://fossies.org/linux/misc/old/libelf-0.8.13.tar.gz/"
  url "https://cdn.netbsd.org/pub/pkgsrc/distfiles/libelf-0.8.13.tar.gz"
  mirror "https://fossies.org/linux/misc/old/libelf-0.8.13.tar.gz"
  sha256 "591a9b4ec81c1f2042a97aa60564e0cb79d041c52faa7416acb38bc95bd2c76d"
  license "LGPL-2.0-or-later"
  revision 1

  livecheck do
    skip "No longer developed or maintained"
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "e823126ce5f1a6783ee5865430312cd4877560586fe17fae30a97bf6172cd76c"
  end

  # Commented out while this formula still has dependents.
  # deprecate! date: "2019-05-17", because: :unmaintained # and upstream site is gone

  depends_on "autoconf" => :build
  depends_on "automake" => :build
  depends_on "libtool" => :build
  # `libelf` should never be used on Linux as `elfutils` is available.

  def install
    # Workaround for ancient config files not recognising aarch64 macos.
    am = Formula["automake"]
    am_share = am.opt_share/"automake-#{am.version.major_minor}"
    %w[config.guess config.sub].each do |fn|
      cp am_share/fn, fn
    end

    system "autoreconf", "--force", "--install", "--verbose"
    system "./configure", "--disable-compat", *std_configure_args
    # Use separate steps; there is a race in the Makefile.
    system "make"
    system "make", "install"
  end

  test do
    elf_content =  "7F454C460101010000000000000000000200030001000000548004083" \
                   "4000000000000000000000034002000010000000000000001000000000000000080040" \
                   "80080040874000000740000000500000000100000B00431DB43B96980040831D2B20CC" \
                   "D8031C040CD8048656C6C6F20776F726C640A"
    File.binwrite(testpath/"elf", [elf_content].pack("H*"))

    (testpath/"test.c").write <<~C
      #include <gelf.h>
      #include <fcntl.h>
      #include <stdio.h>

      int main(void) {
        GElf_Ehdr ehdr;
        int fd = open("elf", O_RDONLY, 0);
        if (elf_version(EV_CURRENT) == EV_NONE) return 1;
        Elf *e = elf_begin(fd, ELF_C_READ, NULL);
        if (elf_kind(e) != ELF_K_ELF) return 1;
        if (gelf_getehdr(e, &ehdr) == NULL) return 1;
        printf("%d-bit ELF\\n", gelf_getclass(e) == ELFCLASS32 ? 32 : 64);
        return 0;
      }
    C

    system ENV.cc, "test.c", "-L#{lib}", "-I#{include}/libelf",
                   "-lelf", "-o", "test"
    assert_match "32-bit ELF", shell_output("./test")
  end
end
