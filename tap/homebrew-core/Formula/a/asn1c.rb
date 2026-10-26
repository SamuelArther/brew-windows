class Asn1c < Formula
  desc "Compile ASN.1 specifications into C source code"
  homepage "https://lionet.info/asn1c/blog"
  url "https://github.com/vlm/asn1c/releases/download/v0.9.29/asn1c-0.9.29.tar.gz"
  sha256 "a8b7b60475357285ac7250f0f3452d0c7555abffa50e1226a0a671df715cc9ff"
  license "BSD-2-Clause"

  bottle do
    sha256 cellar: :any, x86_64_windows: "44605dbc3ac2ffee5a297248469dacc62e04b8409dbdf90112d0fe4d6204674a"
  end

  head do
    url "https://github.com/vlm/asn1c.git", branch: "master"

    depends_on "autoconf" => :build
    depends_on "automake" => :build
    depends_on "libtool" => :build
  end

  deny_network_access!

  def install
    system "autoreconf", "--force", "--install", "--verbose" if build.head?

    args = []
    # Help old config scripts identify arm64 linux
    args << "--build=aarch64-unknown-linux-gnu" if OS.linux? && Hardware::CPU.arm? && Hardware::CPU.is_64_bit?

    system "./configure", "--mandir=#{man}", *args, *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"test.asn1").write <<~ASN1
      MyModule DEFINITIONS ::=
      BEGIN

      MyTypes ::= SEQUENCE {
         myObjectId    OBJECT IDENTIFIER,
         mySeqOf       SEQUENCE OF MyInt,
         myBitString   BIT STRING {
                              muxToken(0),
                              modemToken(1)
                     }
      }

      MyInt ::= INTEGER (0..65535)

      END
    ASN1

    system bin/"asn1c", "test.asn1"
  end
end
