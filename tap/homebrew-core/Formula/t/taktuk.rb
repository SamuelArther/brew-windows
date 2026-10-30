class Taktuk < Formula
  desc "Deploy commands to (a potentially large set of) remote nodes"
  homepage "https://taktuk.gitlabpages.inria.fr/"
  url "https://deb.debian.org/debian/pool/main/t/taktuk/taktuk_3.7.8.orig.tar.gz"
  sha256 "f674edd33d27760b1ee6d41abf4542e07061d049405f0203d151e1af74be9b5c"
  license "GPL-2.0-or-later"
  compatibility_version 1

  livecheck do
    url "https://deb.debian.org/debian/pool/main/t/taktuk/"
    regex(/href=.*?taktuk[._-]v?(\d+(?:\.\d+)+)\.orig\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "a4b12572ff052f7a2c1626c5dda49d9babec16332293692906b3eb9c6d6ed904"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build
  depends_on "libtool" => :build

  uses_from_macos "perl"

  def install
    system "autoreconf", "--force", "--install", "--verbose"
    system "./configure", *std_configure_args
    system "make"
    ENV.deparallelize
    system "make", "install", "INSTALLSITEMAN3DIR=#{man3}"
  end

  test do
    system bin/"taktuk", "quit"
  end
end
