class Libdivecomputer < Formula
  desc "Library for communication with various dive computers"
  homepage "https://www.libdivecomputer.org/"
  url "https://www.libdivecomputer.org/releases/libdivecomputer-0.9.0.tar.gz"
  sha256 "a7b80b9083a2113a43280ee7b51d48d66ea5a779fc3fee57df7c451da0251c65"
  license "LGPL-2.1-or-later"
  head "https://github.com/libdivecomputer/libdivecomputer.git", branch: "master"

  livecheck do
    url "https://www.libdivecomputer.org/releases/"
    regex(/href=.*?libdivecomputer[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "e5bd8feed0d81609c66d81a2fef355796fcd09dbab9946a48da08bd445f02344"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build
  depends_on "libtool" => :build
  depends_on "pkgconf" => :build
  depends_on "libusb"

  def install
    system "autoreconf", "--force", "--install", "--verbose" if build.head?
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <stdio.h> // for NULL macro
      #include <libdivecomputer/context.h>
      #include <libdivecomputer/descriptor.h>
      #include <libdivecomputer/iterator.h>
      int main(int argc, char *argv[]) {
        dc_iterator_t *iterator;
        dc_descriptor_t *descriptor;
        dc_descriptor_iterator(&iterator);
        while (dc_iterator_next(iterator, &descriptor) == DC_STATUS_SUCCESS)
        {
          dc_descriptor_free(descriptor);
        }
        dc_iterator_free(iterator);
        return 0;
      }
    C
    flags = %W[
      -I#{include}
      -L#{lib}
      -ldivecomputer
    ]
    system ENV.cc, "-v", "test.c", "-o", "test", *flags
    system "./test"
  end
end
