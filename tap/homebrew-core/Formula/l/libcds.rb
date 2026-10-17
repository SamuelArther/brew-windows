class Libcds < Formula
  desc "C++ library of Concurrent Data Structures"
  homepage "https://libcds.sourceforge.net/doc/cds-api/index.html"
  url "https://github.com/khizmax/libcds/archive/refs/tags/v2.3.3.tar.gz"
  sha256 "f090380ecd6b63a3c2b2f0bdb27260de2ccb22486ef7f47cc1175b70c6e4e388"
  license "BSL-1.0"
  revision 1

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "4dfc8f47e01c696ab11f653f4c9b47508df70b94c1f2c9d9929f9586f6792a1b"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    # Change the install library directory for x86_64 arch to `lib`
    inreplace "CMakeLists.txt", "set(LIB_SUFFIX \"64\")", ""

    system "cmake", "-S", ".", "-B", "_build", "-DCMAKE_POLICY_VERSION_MINIMUM=3.5", *std_cmake_args
    system "cmake", "--build", "_build"
    system "cmake", "--install", "_build"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <cds/init.h>

      int main() {
        cds::Initialize();
        cds::threading::Manager::attachThread();
        cds::Terminate();
        return 0;
      }
    CPP

    system ENV.cxx, "-std=c++11", "test.cpp", "-o", "test", "-L#{lib}", "-lcds", "-lpthread"
    system "./test"
  end
end
