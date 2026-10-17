class Libplctag < Formula
  desc "Portable and simple API for accessing AB PLC data over Ethernet"
  homepage "https://github.com/libplctag/libplctag"
  url "https://github.com/libplctag/libplctag/archive/refs/tags/v2.7.2.tar.gz"
  sha256 "09057d893a418e10c977267fe57a0195a4b8a4d7e512acc2d9ccef0314823056"
  license any_of: ["LGPL-2.0-or-later", "MPL-2.0"]

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "e951af95ad7e950cdba63d44bd32641c408480e35513bfbd1dfcb8ff2f5596e0"
  end

  depends_on "cmake" => :build

  # Backport "ab_server: scope CIP_SRV_UNCONNECTED_SEND case body"
  patch do
    url "https://github.com/libplctag/libplctag/commit/b19081bc35bd93893b343091add7d638ee3fe532.patch?full_index=1"
    sha256 "7b84abb42b4ba7b72b2b391f471acc56719481b590634cd1afd0576e7d1e1432"
    type :backport
    resolves "https://github.com/libplctag/libplctag/pull/618"
  end

  deny_network_access!
  def install
    # Vendored libyafl uses MAP_ANONYMOUS which requires _GNU_SOURCE on Linux
    ENV.append "CFLAGS", "-D_GNU_SOURCE" if OS.linux?
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <stdlib.h>
      #include <libplctag.h>

      int main(int argc, char **argv) {
        int32_t tag = plc_tag_create("protocol=ab_eip&gateway=192.168.1.42&path=1,0&cpu=LGX&elem_size=4&elem_count=10&name=myDINTArray", 1);
        if (!tag) abort();
        plc_tag_destroy(tag);
        return 0;
      }
    C

    system ENV.cc, "test.c", "-L#{lib}", "-lplctag", "-o", "test"
    system "./test"
  end
end
