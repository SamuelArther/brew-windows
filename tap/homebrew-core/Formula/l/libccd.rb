class Libccd < Formula
  desc "Collision detection between two convex shapes"
  homepage "https://github.com/danfis/libccd"
  url "https://github.com/danfis/libccd/archive/refs/tags/v2.1.tar.gz"
  sha256 "542b6c47f522d581fbf39e51df32c7d1256ac0c626e7c2b41f1040d4b9d50d1e"
  license "BSD-3-Clause"
  revision 1
  compatibility_version 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "f4ea07c6b9d7051b0e3d4fc94e4649979450ef69d983e6ed6cccd8c25690d747"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    args = %w[
      -DENABLE_DOUBLE_PRECISION=ON
    ]
    # Workaround to build with CMake 4
    args << "-DCMAKE_POLICY_VERSION_MINIMUM=3.5"
    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <assert.h>
      #include <ccd/config.h>
      #include <ccd/vec3.h>
      int main() {
      #ifndef CCD_DOUBLE
        assert(false);
      #endif
        ccdVec3PointSegmentDist2(
          ccd_vec3_origin, ccd_vec3_origin,
          ccd_vec3_origin, NULL);
        return 0;
      }
    C
    system ENV.cc, "-o", "test", "test.c", "-L#{lib}", "-lccd"
    system "./test"
  end
end
