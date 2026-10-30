class Libmtp < Formula
  desc "Implementation of Microsoft's Media Transfer Protocol (MTP)"
  homepage "https://libmtp.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/libmtp/libmtp/1.1.23/libmtp-1.1.23.tar.gz"
  sha256 "74a2b6e8cb4a0304e95b995496ea3ac644c29371649b892b856e22f12a0bdeed"
  license "LGPL-2.1-or-later"
  compatibility_version 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "c2ba48f9dfcd63239479779265ccfa60e4e0075230cb7f2bd485b24c82e996d3"
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
