class Cvsutils < Formula
  desc "CVS utilities for use in working directories"
  homepage "https://www.red-bean.com/cvsutils/"
  url "https://www.red-bean.com/cvsutils/releases/cvsutils-0.2.6.tar.gz"
  sha256 "174bb632c4ed812a57225a73ecab5293fcbab0368c454d113bf3c039722695bb"
  license "GPL-2.0-or-later"

  livecheck do
    url "https://www.red-bean.com/cvsutils/releases/"
    regex(/href=.*?cvsutils[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "87c4d20826037f3e72fc396c3f471391ab22c751e221abcd2a55d3da849db80c"
  end

  uses_from_macos "perl"

  deny_network_access!

  def install
    ENV["CONFIG_SHELL"] = "/bin/bash" # for all bottle
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    system bin/"cvsu", "--help"
  end
end
