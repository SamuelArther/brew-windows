class Libmtp < Formula
  desc "Implementation of Microsoft's Media Transfer Protocol (MTP)"
  homepage "https://libmtp.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/libmtp/libmtp/1.1.23/libmtp-1.1.23.tar.gz"
  sha256 "74a2b6e8cb4a0304e95b995496ea3ac644c29371649b892b856e22f12a0bdeed"
  license "LGPL-2.1-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "6a7965cca5a605f1c2d80c8c192727578e52426574d9d3a17baaa3332b407961"
  end

  depends_on "pkgconf" => :build
  depends_on "libusb"

  def install
    system "./configure", "--disable-mtpz",
                          "--disable-silent-rules",
                          "--with-udev=#{lib}/udev",
                          *std_configure_args
    system "make", "install"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mtp-getfile")
  end
end
