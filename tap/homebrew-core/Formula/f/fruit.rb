class Fruit < Formula
  desc "Dependency injection framework for C++"
  homepage "https://github.com/google/fruit/wiki"
  url "https://github.com/google/fruit/archive/refs/tags/v3.7.1.tar.gz"
  sha256 "ed4c6b7ebfbf75e14a74e21eb74ce2703b8485bfc9e660b1c36fb7fe363172d0"
  license "Apache-2.0"
  compatibility_version 1

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e41986f174727a97baec1415f222934b45e5716f2287243403d153128be3bf56"
  end

  depends_on "cmake" => :build

  # Update cmake_minimum_required for compatibility with CMake
  # Remove on next release.
  patch do
    url "https://github.com/google/fruit/commit/b731fdb6426b07bd6674d2d9a057ad13c8e247e7.patch?full_index=1"
    sha256 "dacdf25c966dba2df55526c3c77a036ef48ac1f8fc7a53b17c0e87492963d0f7"
    type :backport
  end

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "_build", "-DFRUIT_USES_BOOST=False", *std_cmake_args
    system "cmake", "--build", "_build"
    system "cmake", "--install", "_build"

    pkgshare.install "examples/hello_world/main.cpp"
  end

  test do
    cp_r pkgshare/"main.cpp", testpath
    system ENV.cxx, "main.cpp", "-I#{include}", "-L#{lib}",
           "-std=c++11", "-lfruit", "-o", "test"
    system "./test"
  end
end
