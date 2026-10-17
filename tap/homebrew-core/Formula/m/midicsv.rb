class Midicsv < Formula
  desc "Convert MIDI audio files to human-readable CSV format"
  homepage "https://www.fourmilab.ch/webtools/midicsv/"
  url "https://www.fourmilab.ch/webtools/midicsv/midicsv-1.1.tar.gz"
  sha256 "7c5a749ab5c4ebac4bd7361df0af65892f380245be57c838e08ec6e4ac9870ef"
  license :public_domain

  livecheck do
    url :homepage
    regex(/href=.*?midicsv[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/midicsv-1.1"
    sha256 cellar: :any, x86_64_windows: "124524a74ba13369315af293cbb9b0e98c1cb9bfdcf461b40115322636a897f4"
  end

  def install
    system "make"
    system "make", "check"
    system "make", "install", "INSTALL_DEST=#{prefix}"
    share.install prefix/"man"
  end

  test do
    system bin/"midicsv", "-u"
  end
end
