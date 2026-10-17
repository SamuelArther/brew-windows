class Ndiff < Formula
  desc "Virtual package provided by nmap"
  homepage "https://www.math.utah.edu/~beebe/software/ndiff/"
  url "https://ftp.math.utah.edu/pub/misc/ndiff-2.00.tar.gz"
  sha256 "f2bbd9a2c8ada7f4161b5e76ac5ebf9a2862cab099933167fe604b88f000ec2c"
  license "GPL-2.0-or-later"

  livecheck do
    url :homepage
    regex(/href=.*?ndiff[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "1adb0577f12cf14b185ed9fdd0eb73a953ef55650ae96ce1f5a4456a0c3572be"
  end

  conflicts_with "cern-ndiff", "nmap", because: "both install `ndiff` binaries"

  def install
    # workaround for newer clang
    ENV.append_to_cflags "-Wno-implicit-int" if DevelopmentTools.clang_build_version >= 1403

    ENV.deparallelize
    # Install manually as the `install` make target is crufty
    system "./configure", "--prefix=.", "--mandir=."
    mkpath "bin"
    mkpath "man/man1"
    system "make", "install"
    bin.install "bin/ndiff"
    man1.install "man/man1/ndiff.1"
  end

  test do
    system bin/"ndiff", "--help"
  end
end
