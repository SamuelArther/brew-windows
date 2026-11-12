class Frei0r < Formula
  desc "Minimalistic plugin API for video effects"
  homepage "https://frei0r.dyne.org/"
  url "https://github.com/dyne/frei0r/archive/refs/tags/v3.7.1.tar.gz"
  sha256 "5543917de48d2e8cc2fa02794240a3613447d00458bc2cf606aef20f4b8da515"
  license "GPL-2.0-or-later"
  compatibility_version 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "5a9d5e33d6f8dfa00c47f6ee7ad8f349b10eb9ec91f30bf69030055a79d71e5f"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    # Skip the Linux-only `shadert0y` filter, which needs OpenGL/EGL from `mesa`
    inreplace "src/filter/CMakeLists.txt", "add_subdirectory (shadert0y)", ""

    args = %w[
      -DWITHOUT_OPENCV=ON
      -DWITHOUT_GAVL=ON
      -DWITHOUT_CAIRO=ON
    ]

    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <frei0r.h>

      int main()
      {
        int mver = FREI0R_MAJOR_VERSION;
        if (mver != 0) {
          return 0;
        } else {
          return 1;
        }
      }
    C
    system ENV.cc, "-L#{lib}", "test.c", "-o", "test"
    system "./test"
  end
end
