class Liblbfgs < Formula
  desc "C library for limited-memory BFGS optimization algorithm"
  homepage "https://www.chokkan.org/software/liblbfgs"
  url "https://github.com/chokkan/liblbfgs/archive/refs/tags/v1.10.tar.gz"
  sha256 "95c1997e6c215c58738f5f723ca225d64c8070056081a23d636160ff2169bd2f"
  license "MIT"
  compatibility_version 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "22084261975ad2f9b07e338ad9e0fd0c4f3c2a4189e0561c65ec8100bdb314ab"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build
  depends_on "libtool" => :build

  def install
    system "autoreconf", "--force", "--install", "--verbose"
    system "./configure", *std_configure_args, "--disable-silent-rules"
    system "make", "install"
    pkgshare.install "sample/sample.c"
  end

  test do
    cp pkgshare/"sample.c", testpath/"sample.c"
    system ENV.cc, "sample.c", "-I#{include}", "-L#{lib}", "-llbfgs", "-o", "./test"
    output = shell_output("./test")

    assert_match "L-BFGS optimization terminated with status code = 0", output
    assert_match "fx = 0.000000, x[0] = 1.000000, x[1] = 1.000000", output
  end
end
