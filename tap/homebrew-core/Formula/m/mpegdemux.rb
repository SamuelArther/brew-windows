class Mpegdemux < Formula
  desc "MPEG1/2 system stream demultiplexer"
  homepage "http://www.hampa.ch/mpegdemux/"
  url "https://deb.debian.org/debian/pool/main/m/mpegdemux/mpegdemux_0.1.5.orig.tar.gz"
  mirror "http://www.hampa.ch/mpegdemux/mpegdemux-0.1.5.tar.gz"
  sha256 "05015755d45e50cbd3018baeaa8abcedc003b1162fa28237a72ab25c1bc00023"
  license "GPL-2.0-only"

  livecheck do
    url :homepage
    regex(/href=.*?mpegdemux[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "09a5d8c33c6d56c124884414640f84113902535f1c8bb086904ef503ce2b2e90"
  end

  def install
    system "./configure", "--prefix=#{prefix}"
    system "make"
    system "make", "install"
  end

  test do
    system bin/"mpegdemux", "--help"
  end
end
