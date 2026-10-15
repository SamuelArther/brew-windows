class Libmysofa < Formula
  desc "Reader for AES SOFA files to get better HRTFs"
  homepage "https://github.com/hoene/libmysofa"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libmysofa-1.3.5-1-any.pkg.tar.zst"
  sha256 "ff8ff472d14376194fcf57d51adb56033c30d6d0b81824e193b03b8a4b105bdd"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/hoene/libmysofa/archive/refs/tags/v1.3.5.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"mysofa.h"
    assert_path_exists lib/"libmysofa.dll.a"
    assert_path_exists bin/"libmysofa.dll"
  end
end
