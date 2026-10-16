class Swfmill < Formula
  desc "Processor of xml2swf and swf2xml"
  homepage "https://www.swfmill.org/"
  url "https://www.swfmill.org/releases/swfmill-0.3.6.tar.gz"
  sha256 "db24f63963957faec02bb14b8b61cdaf7096774f8cfdeb9d3573e2e19231548e"
  license "GPL-2.0-only"
  revision 1

  # adobe flash player EOL 12/31/2020, https://www.adobe.com/products/flashplayer/end-of-life-alternative.html
  deprecate! date: "2025-01-12", because: :unmaintained
  disable! date: "2026-01-12", because: :unmaintained

  depends_on "pkgconf" => :build
  depends_on "freetype"
  depends_on "libpng"

  uses_from_macos "libxml2"
  uses_from_macos "libxslt"
  uses_from_macos "zlib"

  deny_network_access!

  def install
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    false
  end
end
