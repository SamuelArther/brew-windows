class Jcal < Formula
  desc "UNIX-cal-like tool to display Jalali calendar"
  homepage "https://savannah.nongnu.org/projects/jcal/"
  url "https://download.savannah.gnu.org/releases/jcal/jcal-0.6.0.tar.gz"
  sha256 "1ff30b55c2aaa8483ce13674c1ed08c4a6cca31bcaa598bd23889c17dbb2a419"
  license "GPL-3.0-or-later"

  livecheck do
    url "https://download.savannah.gnu.org/releases/jcal/"
    regex(/href=.*?jcal[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "d9acc14ba4f115bc9ae04c38c102d7e59902772fc481b7c5586e6faf022fd3f6"
  end

  def install
    system "./configure", "--prefix=#{prefix}",
                          "--disable-debug",
                          "--disable-dependency-tracking"
    system "make"
    system "make", "install"
  end

  test do
    system bin/"jcal", "-y"
    system bin/"jdate"
  end
end
