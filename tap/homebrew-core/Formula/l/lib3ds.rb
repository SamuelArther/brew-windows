class Lib3ds < Formula
  desc "Library for managing 3D-Studio Release 3 and 4 '.3DS' files"
  homepage "https://code.google.com/archive/p/lib3ds/"
  url "https://storage.googleapis.com/google-code-archive-downloads/v2/code.google.com/lib3ds/lib3ds-1.3.0.zip"
  sha256 "f5b00c302955a67fa5fb1f2d3f2583767cdc61fdbc6fd843c0c7c9d95c5629e3"
  license "LGPL-2.1-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "f0e943c507d6ffcd729ece6682108fdd1fe1ecf2f85a776c5a3fbbdba3ccbda7"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build
  depends_on "libtool" => :build

  deny_network_access!

  def install
    # Run autoreconf to regenerate the configure script and update outdated macros.
    # This ensures that the build system is properly configured on both macOS
    # (avoiding issues like flat namespace conflicts) and Linux (where outdated
    # config scripts may fail to detect the correct build type).
    system "autoreconf", "--force", "--install", "--verbose"

    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    # create a raw empty 3ds file.
    (testpath/"test.3ds").write("\x4d\x4d\x06\x00\x00\x00")
    system bin/"3dsdump", "test.3ds"
  end
end
