class Lhasa < Formula
  desc "LHA implementation to decompress .lzh and .lzs archives"
  homepage "https://fragglet.github.io/lhasa/"
  url "https://github.com/fragglet/lhasa/releases/download/v0.6.0/lhasa-0.6.0.tar.gz"
  sha256 "9840154367f73e9d9c3196f944a121ab4d398d84e921c8fe8fca8a931274aed7"
  license "ISC"

  bottle do
    sha256 cellar: :any, x86_64_windows: "523bc998f51a04070eba798aba27c5ce79a0b8053d2fda57be77f86d047e401d"
  end

  head do
    url "https://github.com/fragglet/lhasa.git", branch: "master"

    depends_on "autoconf" => :build
    depends_on "automake" => :build
    depends_on "libtool" => :build
  end

  depends_on "pkgconf" => :build

  deny_network_access!

  def install
    configure = build.head? ? "./autogen.sh" : "./configure"

    system configure, "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    data = [
      %w[
        31002d6c68302d0400000004000000f59413532002836255050000865a060001666f6f0
        50050a4810700511400f5010000666f6f0a00
      ].join,
    ].pack("H*")

    pipe_output("#{bin}/lha x -", data)
    assert_equal "foo\n", (testpath/"foo").read
  end
end
