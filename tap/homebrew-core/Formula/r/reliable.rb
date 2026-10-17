class Reliable < Formula
  desc "Simple packet acknowledgement system for UDP-based protocols"
  homepage "https://github.com/mas-bandwidth/reliable"
  url "https://github.com/mas-bandwidth/reliable/archive/refs/tags/v1.4.5.tar.gz"
  sha256 "102627cd8643b97865413053f443cd891134a6d0e1d20ab3939e8b09f5c54356"
  license "BSD-3-Clause"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/reliable-1.4.5"
    sha256 cellar: :any, x86_64_windows: "0f572a0275ca836bebfd9d917be67808f176c35bd7d1d90c9b765336a923e9bf"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", "-DBUILD_SHARED_LIBS=ON", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <reliable.h>
      #include <stdio.h>

      int main() {
        if (reliable_init() != RELIABLE_OK) {
          return 1;
        }
        printf("%s", RELIABLE_VERSION_FULL);
        reliable_term();
        return 0;
      }
    C
    system ENV.cc, "test.c", "-I#{include}", "-L#{lib}", "-lreliable", "-o", "test"
    assert_equal version.to_s, shell_output("./test")
  end
end
