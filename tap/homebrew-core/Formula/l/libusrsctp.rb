class Libusrsctp < Formula
  desc "Portable SCTP userland stack"
  homepage "https://github.com/sctplab/usrsctp"
  license "BSD-3-Clause"
  revision 1
  compatibility_version 1
  head "https://github.com/sctplab/usrsctp.git", branch: "master"

  stable do
    url "https://github.com/sctplab/usrsctp/archive/refs/tags/0.9.5.0.tar.gz"
    sha256 "260107caf318650a57a8caa593550e39bca6943e93f970c80d6c17e59d62cd92"

    # Fix minimum required version to configure with CMake 4
    # Remove with `stable` block on next release.
    patch do
      url "https://github.com/sctplab/usrsctp/commit/7569d2ce1e8658534369ad9726ca62139211db84.patch?full_index=1"
      sha256 "6d37314f524ff301546f1e53b605dd276837a984545c6dc1cb21ad87cbd49f6b"
      type :backport
      resolves "https://github.com/sctplab/usrsctp/pull/724"
    end
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "66241d0cfa247055594389c6cbd0871af7d9deccaf9d0ae1b0d21a9a74f44c43"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    # Newer GCC warns more (-Wunused-but-set-variable) than usrsctp's -Werror allows.
    args = OS.windows? ? ["-Dsctp_werror=OFF"] : []
    system "cmake", "-S", ".", "-B", "build", "-Dsctp_build_shared_lib=ON", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <unistd.h>
      #include <usrsctp.h>
      int main() {
        usrsctp_init(0, NULL, NULL);
        return 0;
      }
    C
    system ENV.cc, "test.c", "-L#{lib}", "-lusrsctp", "-lpthread", "-o", "test"
    system "./test"
  end
end
