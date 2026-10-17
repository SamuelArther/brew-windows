class MipselLinuxGnuBinutils < Formula
  desc "GNU Binutils for mipsel-linux-gnu cross development"
  homepage "https://www.gnu.org/software/binutils/"
  url "https://ftpmirror.gnu.org/binutils/binutils-2.47.tar.bz2"
  mirror "https://ftp.gnu.org/gnu/binutils/binutils-2.47.tar.bz2"
  sha256 "3068128c75cda9f898ccb4211d360246e8e195ffcc9dfb655b23ae23a54800e8"
  license "GPL-3.0-or-later"

  livecheck do
    formula "binutils"
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "cc1ff5c0b2a83c09e7ca0e2889d9951b534732860e9853719485531d04db7f1d"
  end

  depends_on "pkgconf" => :build
  depends_on "zstd"

  on_system :linux, macos: :ventura_or_newer do
    depends_on "texinfo" => :build
  end

  on_linux do
    depends_on "zlib-ng-compat"
  end

  def install
    target = "mipsel-linux-gnu"
    system "./configure", "--target=#{target}",
                          "--infodir=#{info/target}",
                          "--with-system-zlib",
                          "--with-zstd",
                          "--disable-nls",
                          *std_configure_args(libdir: lib/target)
    system "make"
    system "make", "install"
  end

  test do
    (testpath/"test-s.s").write <<~ASM
      .section .text, "ax"
      .set noat
      .globl _start
      _start:
          addiu $v0, $zero, 0
          j $ra
    ASM

    system bin/"mipsel-linux-gnu-as", "-o", "test-s.o", "test-s.s"
    assert_match "file format elf32-tradlittlemips",
                 shell_output("#{bin}/mipsel-linux-gnu-objdump -a test-s.o")
    assert_match "f()", shell_output("#{bin}/mipsel-linux-gnu-c++filt _Z1fv")
  end
end
