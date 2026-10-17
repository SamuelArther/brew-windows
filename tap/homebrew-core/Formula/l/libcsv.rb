class Libcsv < Formula
  desc "CSV library in ANSI C89"
  homepage "https://sourceforge.net/projects/libcsv/"
  url "https://downloads.sourceforge.net/project/libcsv/libcsv/libcsv-3.0.3/libcsv-3.0.3.tar.gz"
  sha256 "d9c0431cb803ceb9896ce74f683e6e5a0954e96ae1d9e4028d6e0f967bebd7e4"
  license "LGPL-2.1-or-later"

  # Fix -flat_namespace being used on Big Sur and later.
  patch do
    file "Patches/libtool/configure-pre-0.4.2.418-big_sur.diff"
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/libcsv-3.0.3"
    sha256 cellar: :any, x86_64_windows: "3f3a90247179772d0a8692f57a60a85b4fc38aab7fe1e40867dbcb621138c6c9"
  end

  def install
    args = []
    # Help old config scripts identify arm64 linux
    args << "--build=aarch64-unknown-linux-gnu" if OS.linux? && Hardware::CPU.arm? && Hardware::CPU.is_64_bit?

    system "./configure", *args, *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <csv.h>
      int main(void) {
        struct csv_parser p;
        csv_init(&p, CSV_STRICT);
      }
    C
    system ENV.cc, "test.c", "-I#{include}", "-L#{lib}", "-lcsv", "-o", "test"
    system "./test"
  end
end
