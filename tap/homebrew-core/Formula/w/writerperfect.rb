class Writerperfect < Formula
  desc "Library for importing WordPerfect documents"
  homepage "https://sourceforge.net/p/libwpd/wiki/writerperfect/"
  url "https://downloads.sourceforge.net/project/libwpd/writerperfect/writerperfect-0.9.6/writerperfect-0.9.6.tar.xz"
  sha256 "1fe162145013a9786b201cb69724b2d55ff2bf2354c3cd188fd4466e7fc324e6"
  license any_of: ["LGPL-2.1-or-later", "MPL-2.0"]

  livecheck do
    url :stable
    regex(%r{url=.*?/writerperfect[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "1f8722f4933e83d5802e04eff34f9a5b9fbeeb40668c98dcab952d59b3343b18"
  end

  depends_on "pkgconf" => :build
  depends_on "libodfgen"
  depends_on "librevenge"
  depends_on "libwpd"
  depends_on "libwpg"
  depends_on "libwps"

  deny_network_access!

  def install
    system "./configure", "--disable-silent-rules", *std_configure_args
    system "make", "install"
  end

  test do
    # Minimal WordPerfect 5.1 document: 16-byte "\xFFWPC" header followed by the text
    header = ["\xFFWPC".b, 16, 1, 0x0a, 0, 1, 0, 0].pack("a4VCCCCvv")
    (testpath/"test.wpd").binwrite header + "Hello Homebrew"

    output = shell_output("#{bin}/wpd2odt --stdout test.wpd")
    assert_match 'office:mimetype="application/vnd.oasis.opendocument.text"', output
    assert_match %r{<text:p [^>]*><text:span [^>]*>Hello Homebrew</text:span></text:p>}, output

    system bin/"wpd2odt", "test.wpd", "test.odt"
    odt = (testpath/"test.odt").binread
    assert_equal "PK\x03\x04", odt[0, 4]
    assert_match "mimetypeapplication/vnd.oasis.opendocument.text", odt
  end
end
