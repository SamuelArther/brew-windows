class Woff2 < Formula
  desc "Utilities to create and convert Web Open Font File (WOFF) files"
  homepage "https://github.com/google/woff2"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-woff2-1.0.2-5-any.pkg.tar.zst"
  sha256 "00a602d2af3cbf68ce98ee93db5fcd9dd8c0c12aef6812c1633aab914385f3a2"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/google/woff2/archive/refs/tags/v1.0.2.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"woff2/decode.h"
    assert_path_exists lib/"libwoff2common.dll.a"
    assert_path_exists bin/"libwoff2common.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libwoff2common.pc").read
  end
end
