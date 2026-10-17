class Libcaption < Formula
  desc "Free open-source CEA608 / CEA708 closed-caption encoder/decoder"
  homepage "https://github.com/szatmary/libcaption"
  url "https://github.com/szatmary/libcaption/archive/refs/tags/v0.8.tar.gz"
  sha256 "8567765a457de43a6e834502cf42fd0622901428d9820c73495df275e01cb904"
  license "MIT"
  head "https://github.com/szatmary/libcaption.git", branch: "develop"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/libcaption-0.8"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "419a2da917aaefaecc4cfa6e3016b026d27c28dfac3749219f151988b2257201"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    ENV["CMAKE_POLICY_VERSION_MINIMUM"] = "3.5"
    system "cmake", "-S", ".", "-B", "build",
                    "-DBUILD_EXAMPLES=OFF",
                    "-DCMAKE_POSITION_INDEPENDENT_CODE=ON",
                    "-DBUILD_SHARED_LIBS=ON",
                    *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <caption/cea708.h>
      int main(void) {
        caption_frame_t ccframe;
        caption_frame_init(&ccframe);
        return 0;
      }
    C
    system ENV.cc, "test.c", "-I#{include}", "-L#{lib}", "-lcaption", "-o", "test"
    system "./test"
  end
end
