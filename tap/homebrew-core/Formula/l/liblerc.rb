class Liblerc < Formula
  desc "Esri LERC library (Limited Error Raster Compression)"
  homepage "https://github.com/Esri/lerc"
  url "https://github.com/Esri/lerc/archive/refs/tags/v4.2.0.tar.gz"
  sha256 "a1fb593ed1fcb5b38800caf3c4454f872745202e961d00d745e53d81447e17c9"
  license "Apache-2.0"
  compatibility_version 1

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/liblerc-4.2.0"
    sha256 cellar: :any, x86_64_windows: "81f577b56262eb5e405ba983bc55ae123c64707a0bc136b964973ff9c22408bd"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cc").write <<~CPP
      #include <Lerc_c_api.h>
      #include <Lerc_types.h>
      int main() {
        const int infoArrSize = (int)LercNS::InfoArrOrder::_last;
        const int dataRangeArrSize = (int)LercNS::DataRangeArrOrder::_last;
        lerc_status hr(0);

        return 0 ;
      }
    CPP

    system ENV.cxx, "test.cc", "-std=gnu++17",
                    "-I#{include}",
                    "-L#{lib}",
                    "-lLerc",
                    "-o", "test_liblerc"

    assert_empty shell_output("./test_liblerc")
  end
end
