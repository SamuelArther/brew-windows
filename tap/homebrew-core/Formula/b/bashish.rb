class Bashish < Formula
  desc "Theme environment for text terminals"
  homepage "https://bashish.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/bashish/bashish/2.2.4/bashish-2.2.4.tar.gz"
  sha256 "3de48bc1aa69ec73dafc7436070e688015d794f22f6e74d5c78a0b09c938204b"
  license "GPL-2.0-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "dda2d22c8e8dcfc3917e7509e95f21bedd645d30737db7e0fdf488ca48c80f15"
  end

  depends_on "dialog"

  deny_network_access!

  def install
    system "./configure", *std_configure_args
    system "make"
    system "make", "install"
  end

  test do
    system bin/"bashish", "list"
  end
end
