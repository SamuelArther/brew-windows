class Libebml < Formula
  desc "Sort of a sbinary version of XML"
  homepage "https://www.matroska.org/"
  url "https://dl.matroska.org/downloads/libebml/libebml-1.4.7.tar.xz"
  sha256 "5b08214f929ee54c6187c370f84235fc9d0f2a2258c4d320d68eae6e2bdfd3f7"
  license "LGPL-2.1-or-later"
  head "https://github.com/Matroska-Org/libebml.git", branch: "master"

  livecheck do
    url "https://dl.matroska.org/downloads/libebml/"
    regex(/href=.*?libebml[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "bbd63da6f1d3daf5b591392965f4e95dc3b1e8f0ed41f1d25a9ff43fbc901c0c"
  end

  depends_on "cmake" => :build
  depends_on "utf8cpp" => :build

  deny_network_access!

  def install
    args = %w[-DBUILD_SHARED_LIBS=ON]
    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <ebml/EbmlVoid.h>
      #include <iostream>

      int main() {
        libebml::EbmlVoid void_element;
        void_element.SetSize(1024);

        std::cout << "EbmlVoid element created with size: 1024" << std::endl;
        return 0;
      }
    CPP

    system ENV.cxx, "test.cpp", "-o", "test", "-I#{include}", "-L#{lib}", "-lebml"
    system "./test"
  end
end
