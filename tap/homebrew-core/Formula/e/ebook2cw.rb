class Ebook2cw < Formula
  desc "Converts ebooks to morse code"
  homepage "https://fkurz.net/ham/ebook2cw.html"
  url "https://fkurz.net/ham/ebook2cw/ebook2cw-0.8.5.tar.gz"
  sha256 "571f734f12123b4affbad90b55dd4c9630b254afe343fa621fc5114b9bd25fc3"
  license "GPL-2.0-or-later"

  livecheck do
    url "https://fkurz.net/ham/ebook2cw/"
    regex(/href=.*?ebook2cw[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "05484d4cae6c2761146db69beea70b16f4abc8a3a0fcf6c56151fef364af5f7a"
  end

  depends_on "lame"
  depends_on "libogg"
  depends_on "libvorbis"

  on_macos do
    depends_on "gettext"
  end

  on_linux do
    depends_on "gettext" => :build # for msgfmt
  end

  def install
    system "make", "DESTDIR=#{prefix}"
    system "make", "install", "DESTDIR=#{prefix}"
  end

  test do
    pipe_output("#{bin}/ebook2cw -o test", "test mp3 file generation")
    assert_path_exists testpath/"test0000.mp3"

    pipe_output("#{bin}/ebook2cw -O -o test", "test ogg file generation")
    assert_path_exists testpath/"test0000.ogg"
  end
end
