class OpenclHeaders < Formula
  desc "C language header files for the OpenCL API"
  homepage "https://www.khronos.org/registry/OpenCL/"
  url "https://github.com/KhronosGroup/OpenCL-Headers/archive/refs/tags/v2026.05.29.tar.gz"
  sha256 "d9e6c48357de5002da11ce45de600e0c3ffe6ab4f628a3b9fe2b38603161658a"
  license "Apache-2.0"
  head "https://github.com/KhronosGroup/OpenCL-Headers.git", branch: "main"

  keg_only :shadowed_by_macos, "macOS provides OpenCL.framework"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/opencl-headers-2026.05.29"
    sha256 cellar: :any, x86_64_windows: "a0857c00d0257cdd7e74b3520fc0ceabbf91fc321e287e7d93a2161b33dfa431"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"

    # Build an `:all` bottle by adding symlinks same as macOS
    include.install_symlink "CL" => "OpenCL" if OS.linux?
  end

  test do
    (testpath/"test.c").write <<~C
      #include <stdio.h>
      #include <CL/opencl.h>

      int main(void) {
        printf("opencl.h standalone test PASSED.");
        return 0;
      }
    C

    system ENV.cc, "test.c", "-o", "test", "-I#{include}"
    assert_equal "opencl.h standalone test PASSED.", shell_output("./test")
  end
end
