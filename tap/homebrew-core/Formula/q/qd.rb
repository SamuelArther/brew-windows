class Qd < Formula
  desc "C++/Fortran-90 double-double and quad-double package"
  homepage "https://www.davidhbailey.com/dhbsoftware/"
  url "https://www.davidhbailey.com/dhbsoftware/qd-2.3.24.tar.gz"
  sha256 "a47b6c73f86e6421e86a883568dd08e299b20e36c11a99bdfbe50e01bde60e38"
  license "BSD-3-Clause-LBNL"

  # The homepage no longer links to a QD tarball and instead directs users to
  # the GitHub repository, so we check the Git tags.
  livecheck do
    url "https://github.com/BL-highprecision/QD.git"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "7d78a5b31f8e1ab1bf335636636de09cd5a273d9161aca20003c0ec1c2626d76"
  end

  # Drop `autoconf`, `automake`, `libtool` when the patch is removed.
  depends_on "autoconf" => :build
  depends_on "automake" => :build
  depends_on "libtool" => :build
  depends_on "gcc" # for gfortran

  deny_network_access!

  def install
    odie "check if autoreconf line can be removed" if version > "2.3.24"
    # regenerate since the files were generated using automake 1.16.5
    system "autoreconf", "--install", "--force", "--verbose"

    system "./configure", "--enable-shared", *std_configure_args
    system "make", "install"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/qd-config --configure-args")
  end
end
