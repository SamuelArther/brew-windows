class Lhasa < Formula
  desc "LHA implementation to decompress .lzh and .lzs archives"
  homepage "https://fragglet.github.io/lhasa/"
  url "https://github.com/fragglet/lhasa/releases/download/v0.6.0/lhasa-0.6.0.tar.gz"
  sha256 "9840154367f73e9d9c3196f944a121ab4d398d84e921c8fe8fca8a931274aed7"
  license "ISC"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/lhasa-0.6.0"
    sha256 cellar: :any, x86_64_windows: "8425e03b969a8ad7b99d0284870e04ea1b11ca2f7096c8b86efca0c0ecba7abf"
  end

  head do
    url "https://github.com/fragglet/lhasa.git", branch: "master"

    depends_on "autoconf" => :build
    depends_on "automake" => :build
    depends_on "libtool" => :build
  end

  depends_on "pkgconf" => :build

  def install
    if build.head?
      system "./autogen.sh", "--prefix=#{prefix}"
    else
      system "./configure", "--prefix=#{prefix}"
    end
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
