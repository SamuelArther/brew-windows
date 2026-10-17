class Jthread < Formula
  desc "C++ class to make use of threads easy"
  homepage "https://research.edm.uhasselt.be/jori/page/CS/Jthread.html"
  url "https://research.edm.uhasselt.be/jori/jthread/jthread-1.3.3.tar.bz2"
  sha256 "17560e8f63fa4df11c3712a304ded85870227b2710a2f39692133d354ea0b64f"
  license "MIT"

  livecheck do
    url :homepage
    regex(/href=.*?jthread[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/jthread-1.3.3"
    sha256 cellar: :any, x86_64_windows: "817f3d2ec4355986f90ab1385e43dd34a829a005fc0460a4da93c04c1304fdc6"
  end

  depends_on "cmake" => :build
  depends_on "pkgconf" => :build

  deny_network_access!

  def install
    # Workaround to build with CMake 4
    args = %w[-DCMAKE_POLICY_VERSION_MINIMUM=3.5]
    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <jthread/jthread.h>
      using namespace jthread;

      int main() {
        JMutex* jm = new JMutex();
        jm->Init();
        jm->Lock();
        jm->Unlock();
        return 0;
      }
    CPP

    system ENV.cxx, "test.cpp", "-I#{include}", "-L#{lib}", "-ljthread",
                    "-o", "test"
    system "./test"
  end
end
