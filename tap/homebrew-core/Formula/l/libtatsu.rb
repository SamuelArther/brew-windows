class Libtatsu < Formula
  desc "Library handling the communication with Apple's Tatsu Signing Server (TSS)"
  homepage "https://libimobiledevice.org/"
  url "https://github.com/libimobiledevice/libtatsu/releases/download/1.1.0/libtatsu-1.1.0.tar.bz2"
  sha256 "96c8abd97552a4a0c487c701aba9a65d590c338dd331058ffb38304290b696c7"
  license "LGPL-2.1-or-later"
  compatibility_version 1
  head "https://github.com/libimobiledevice/libtatsu.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "dcf94f8dd593b2ad3c3cca53a825cfaaa25f0b838702f8b3e392b4c57fd1a8ec"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build
  depends_on "libtool" => :build
  depends_on "pkgconf" => :build
  depends_on "libplist"

  uses_from_macos "curl"

  def install
    configure = build.head? ? "./autogen.sh" : "./configure"
    system configure, "--disable-silent-rules", *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"test.c").write <<~C
      #include "libtatsu/tss.h"

      int main(int argc, char* argv[]) {
        tss_set_debug_level(0);
        return 0;
      }
    C
    system ENV.cc, "test.c", "-L#{lib}", "-ltatsu", "-o", "test"
    system "./test"
  end
end
