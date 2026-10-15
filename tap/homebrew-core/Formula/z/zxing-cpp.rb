class ZxingCpp < Formula
  desc "Multi-format barcode image processing library written in C++"
  homepage "https://github.com/zxing-cpp/zxing-cpp"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zxing-cpp-3.0.2-3-any.pkg.tar.zst"
  sha256 "828f24e54e53148eaa048f152b7c11fc8434d1ff5a440fb73ac5a7b73536d8cc"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/zxing-cpp/zxing-cpp/releases/download/v3.1.1/zxing-cpp-3.1.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"ZXing/GTIN.h"
    assert_path_exists lib/"libZXing.dll.a"
    assert_path_exists bin/"libZXing.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/zxing.pc").read
  end
end
