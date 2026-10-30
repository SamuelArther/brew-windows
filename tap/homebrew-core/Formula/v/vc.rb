class Vc < Formula
  desc "SIMD Vector Classes for C++"
  homepage "https://github.com/VcDevel/Vc"
  url "https://github.com/VcDevel/Vc/archive/refs/tags/1.4.5.tar.gz"
  sha256 "eb734ef4827933fcd67d4c74aef54211b841c350a867c681c73003eb6d511a48"
  license "BSD-3-Clause"
  compatibility_version 1

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "ac8195773ee66c74beae526089e751df374572c1c9425e5f907496a0efb02624"
  end

  depends_on "cmake" => :build

  def install
    ENV.runtime_cpu_detection
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <Vc/Vc>

      using Vc::float_v;
      using Vec3D = std::array<float_v, 3>;

      float_v scalar_product(Vec3D a, Vec3D b) {
        return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
       }

       int main(){
         return 0;
       }
    CPP
    extra_flags = []
    extra_flags += ["-lm", "-lstdc++"] unless OS.mac?
    system ENV.cc, "test.cpp", "-std=c++11", "-L#{lib}", "-lVc", *extra_flags, "-o", "test"
    system "./test"
  end
end
