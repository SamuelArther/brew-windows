class Libdshconfig < Formula
  desc "Distributed shell library"
  homepage "https://www.netfort.gr.jp/~dancer/software/dsh.html.en"
  url "https://www.netfort.gr.jp/~dancer/software/downloads/libdshconfig-0.20.13.tar.gz"
  sha256 "6f372686c5d8d721820995d2b60d2fda33fdb17cdddee9fce34795e7e98c5384"
  license "GPL-2.0-or-later"

  livecheck do
    url "https://www.netfort.gr.jp/~dancer/software/downloads/"
    regex(/href=.*?libdshconfig[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "fb80993830f495ca1cd97917dee6e268c8bf16797947c458ce884ed634441171"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build
  depends_on "libtool" => :build

  def install
    # Run autoreconf to regenerate the configure script and update outdated macros.
    # This ensures that the build system is properly configured on both macOS
    # (avoiding issues like flat namespace conflicts) and Linux (where outdated
    # config scripts may fail to detect the correct build type).
    system "autoreconf", "--force", "--install", "--verbose"

    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <libdshconfig.h>
      int main(void) {
        return 0;
      }
    C

    system ENV.cc, "test.c", "-I#{include}", "-L#{lib}", "-ldshconfig", "-o", "test"
    system "./test"
  end
end
