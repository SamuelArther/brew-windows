class Dwatch < Formula
  desc "Watch programs and perform actions based on a configuration file"
  homepage "https://siag.nu/dwatch/"
  url "https://siag.nu/pub/dwatch/dwatch-0.1.1.tar.gz"
  sha256 "ba093d11414e629b4d4c18c84cc90e4eb079a3ba4cfba8afe5026b96bf25d007"
  license "GPL-2.0-or-later"

  livecheck do
    url "https://siag.nu/pub/dwatch/"
    regex(/href=.*?dwatch[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/dwatch-0.1.1"
    sha256 cellar: :any, x86_64_windows: "785174ce6010a057bcc57076ae8c60f18b84fe82a6a6afaa66b213462352d075"
  end

  def install
    # Makefile uses cp, not install
    bin.mkpath
    man1.mkpath

    system "make", "install",
                   "CC=#{ENV.cc}",
                   "PREFIX=#{prefix}",
                   "MANDIR=#{man}",
                   "ETCDIR=#{etc}"

    etc.install "dwatch.conf"
  end

  test do
    # '-h' is not actually an option, but it exits 0
    system bin/"dwatch", "-h"
  end
end
