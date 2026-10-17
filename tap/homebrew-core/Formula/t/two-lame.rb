class TwoLame < Formula
  desc "Optimized MPEG Audio Layer 2 (MP2) encoder"
  homepage "https://www.twolame.org/"
  url "https://downloads.sourceforge.net/project/twolame/twolame/0.4.0/twolame-0.4.0.tar.gz"
  sha256 "cc35424f6019a88c6f52570b63e1baf50f62963a3eac52a03a800bb070d7c87d"
  license "LGPL-2.1-or-later"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/two-lame-0.4.0"
    sha256 cellar: :any, x86_64_windows: "74cc069b7800a5947392d07d9e5f084da621cf7797c086e3d2f8bdbc4887f8d3"
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
