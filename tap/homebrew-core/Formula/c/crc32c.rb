class Crc32c < Formula
  desc "Implementation of CRC32C with CPU-specific acceleration"
  homepage "https://github.com/google/crc32c"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-crc32c-1.1.2-1-any.pkg.tar.zst"
  sha256 "4c0170bb0d960035dfc807dbd0f09ac06cb82f73af8ae771eee31a7092d9209e"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/google/crc32c/archive/refs/tags/1.1.2.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"crc32c/crc32c.h"
    assert_path_exists lib/"libcrc32c.dll.a"
    assert_path_exists bin/"libcrc32c.dll"
  end
end
