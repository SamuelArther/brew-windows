class Libquantum < Formula
  desc "C library for the simulation of quantum mechanics"
  homepage "http://www.libquantum.de/"
  url "https://cdn.netbsd.org/pub/pkgsrc/distfiles/libquantum-1.0.0.tar.gz"
  mirror "http://www.libquantum.de/files/libquantum-1.0.0.tar.gz"
  sha256 "b0f1a5ec9768457ac9835bd52c3017d279ac99cc0dffe6ce2adf8ac762997b2c"
  license "GPL-3.0-or-later"
  version_scheme 1

  livecheck do
    url "http://www.libquantum.de/downloads"
    regex(/href=.*?libquantum[._-]v?(\d+\.[02468](?:\.\d+)*)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "062eec3e8b4f9b5fa7723e3f4722199a31f49ee4f4e6e0b552650b9ce10b12cd"
  end

  deny_network_access!

  def install
    args = []
    # Help old config scripts identify arm64 linux
    args << "--build=aarch64-unknown-linux-gnu" if OS.linux? && Hardware::CPU.arm64?

    system "./configure", *args, *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"qtest.c").write <<~C
      #include <stdio.h>
      #include <stdlib.h>
      #include <time.h>
      #include <quantum.h>

      int main ()
      {
        quantum_reg reg;
        int result;
        srand(time(0));
        reg = quantum_new_qureg(0, 1);
        quantum_hadamard(0, &reg);
        result = quantum_bmeasure(0, &reg);
        printf("The Quantum RNG returned %i!\\n", result);
        return 0;
      }
    C
    args = [
      "-O3",
      "-L#{lib}",
      "-lquantum",
    ]
    args << "-fopenmp" if OS.linux?
    system ENV.cc, "qtest.c", *args, "-o", "qtest"
    system "./qtest"
  end
end
