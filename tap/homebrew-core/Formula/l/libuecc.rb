class Libuecc < Formula
  desc "Very small Elliptic Curve Cryptography library"
  homepage "https://git.universe-factory.net/fastd/libuecc"
  url "https://git.universe-factory.net/fastd/libuecc/archive/v7.tar.gz"
  sha256 "80ef381fae912db88a33ebe1b4c7a722b98ed3b1939f75415068b025c5675818"
  license "BSD-2-Clause"
  compatibility_version 1
  head "https://git.universe-factory.net/fastd/libuecc.git", branch: "main"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/libuecc-7"
    sha256 cellar: :any, x86_64_windows: "6665082f5b27e90ea8c6b1b311a9379a8cc22a6e7266ede44f4a66c9e8f43ed8"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    # Workaround to build with CMake 4
    args = %w[-DCMAKE_POLICY_VERSION_MINIMUM=3.5]
    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <stdlib.h>
      #include <libuecc/ecc.h>

      int main(void)
      {
          ecc_int256_t secret;
          ecc_25519_gf_sanitize_secret(&secret, &secret);

          return EXIT_SUCCESS;
      }
    C

    system ENV.cc, "-I#{include}/libuecc-#{version}", "-L#{lib}", "-o", "test", "test.c", "-luecc"
    system "./test"
  end
end
