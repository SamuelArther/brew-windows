class Termrec < Formula
  desc "Record videos of terminal output"
  homepage "https://angband.pl/termrec.html"
  url "https://github.com/kilobyte/termrec/archive/refs/tags/v0.19.tar.gz"
  sha256 "0550c12266ac524a8afb764890c420c917270b0a876013592f608ed786ca91dc"
  license "LGPL-3.0-or-later"
  head "https://github.com/kilobyte/termrec.git", branch: "master"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "e66b678c5ae9fafa59f4d7c5466027ad0a86c1cd37567ecccde0057ba61a4d6c"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build
  depends_on "libtool" => :build
  depends_on "xz"

  on_linux do
    depends_on "zlib-ng-compat"
  end

  deny_network_access!

  def install
    # Work around build error: call to undeclared function 'forkpty'
    # Issue ref: https://github.com/kilobyte/termrec/issues/8
    ENV.append "CFLAGS", "-include util.h" if DevelopmentTools.clang_build_version >= 1403

    system "./autogen.sh"
    system "./configure", "--disable-silent-rules", *std_configure_args
    system "make", "install"
  end

  test do
    system bin/"termrec", "--help"
  end
end
