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
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/cvsutils-0.2.6"
    sha256 cellar: :any, x86_64_windows: "279071f0f743042ca4a1ff67959af405648b151110bd4db841c2c85a676d7e91"
  end

  uses_from_macos "perl"

  def install
    ENV["CONFIG_SHELL"] = "/bin/bash" # for all bottle
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    system bin/"cvsu", "--help"
  end
end
