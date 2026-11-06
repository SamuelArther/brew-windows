class Dtach < Formula
  desc "Emulates the detach feature of screen"
  homepage "https://dtach.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/dtach/dtach/0.9/dtach-0.9.tar.gz"
  sha256 "32e9fd6923c553c443fab4ec9c1f95d83fa47b771e6e1dafb018c567291492f3"
  license "GPL-2.0-or-later"

  deny_network_access!

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "fc7318399be66bef0a6469719a81a7bd692cf6327b48e34825fe660860daa6b0"
  end

  def install
    # Includes <config.h> instead of "config.h", so "." needs to be in the include path.
    ENV.append "CFLAGS", "-I."

    system "./configure", "--disable-debug", "--disable-dependency-tracking",
                          "--prefix=#{prefix}"

    system "make"
    bin.install "dtach"
    man1.install Utils::Gzip.compress("dtach.1")
  end

  test do
    system bin/"dtach", "--help"
  end
end
