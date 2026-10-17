class Never < Formula
  desc "Statically typed, embedded functional programming language"
  homepage "https://never-lang.readthedocs.io/en/latest/"
  url "https://github.com/never-lang/never/archive/refs/tags/v2.3.9.tar.gz"
  sha256 "9ca3ea42738570f128708404e2f7aad35ef2b8b4b178d64508430c675713e41f"
  license "MIT"
  head "https://github.com/never-lang/never.git", branch: "master"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/never-2.3.9"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "9f4e29a4d0f1e4f3f08f4f2357e424647470244200333b5b5aa115d7fd3d8822"
  end

  depends_on "bison" => :build
  depends_on "cmake" => :build

  uses_from_macos "flex" => :build
  uses_from_macos "libffi"

  deny_network_access!

  def install
    ENV.append_to_cflags "-I#{MacOS.sdk_path}/usr/include/ffi" if OS.mac?

    # Workaround for CMake 4 compatibility
    args = %w[-DCMAKE_POLICY_VERSION_MINIMUM=3.5]
    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    bin.install "build/never"
    lib.install "build/libnev.a"
    prefix.install "include"
  end

  test do
    (testpath/"hello.nev").write <<~NEV
      func main() -> int
      {
        prints("Hello World!\\n");
        0
      }
    NEV
    assert_match "Hello World!", shell_output("#{bin}/never -f hello.nev")

    (testpath/"test.c").write <<~C
      #include "object.h"
      void test_one()
      {
        object * obj1 = object_new_float(100.0);
        object_delete(obj1);
      }
      int main(int argc, char * argv[])
      {
        test_one();
        return 0;
      }
    C
    system ENV.cc, "test.c", "-I#{include}", "-L#{lib}", "-lnev", "-o", "test"
    system "./test"
  end
end
