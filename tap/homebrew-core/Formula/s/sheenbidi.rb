class Sheenbidi < Formula
  desc "Fast and stable implementation of the Unicode Bidirectional Algorithm"
  homepage "https://github.com/Tehreer/SheenBidi"
  url "https://github.com/Tehreer/SheenBidi/archive/refs/tags/v3.0.0.tar.gz"
  sha256 "86c56014034739ba39a24c23eb00323b0bf6f737354f665786015fca842af786"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "56e2b9b530c43ab393181939d47d4992c48040ee6070776fe102d30459a31b77"
  end

  depends_on "cmake" => :build
  depends_on "pkgconf" => :build

  deny_network_access!

  def install
    args = [
      "-DBUILD_SHARED_LIBS=ON",
      "-DSB_CONFIG_UNITY=ON",
    ]

    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <SheenBidi/SheenBidi.h>

      int main() {
        const char *version = SBVersionGetString();
        return 0;
      }
    C

    system ENV.cc, "test.c",
                   "-I#{include}",
                   "-L#{lib}", "-lSheenBidi",
                   "-o", "test"
    system "./test"
  end
end
