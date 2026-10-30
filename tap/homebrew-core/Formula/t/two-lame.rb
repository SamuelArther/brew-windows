class TwoLame < Formula
  desc "Optimized MPEG Audio Layer 2 (MP2) encoder"
  homepage "https://www.twolame.org/"
  url "https://downloads.sourceforge.net/project/twolame/twolame/0.4.0/twolame-0.4.0.tar.gz"
  sha256 "cc35424f6019a88c6f52570b63e1baf50f62963a3eac52a03a800bb070d7c87d"
  license "LGPL-2.1-or-later"
  compatibility_version 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "b04c62a0454126da3a7155f409eb3495c7eee7df091ae4eaabf705b36bcdbdd6"
  end

  def install
    system "./configure", "--disable-dependency-tracking",
                          "--prefix=#{prefix}"
    system "make", "install"
    bin.install "simplefrontend/.libs/stwolame"
  end

  test do
    system bin/"stwolame", test_fixtures("test.wav"), "test.mp2"
  end
end
