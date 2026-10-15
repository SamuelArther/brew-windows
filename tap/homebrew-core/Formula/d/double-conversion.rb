class DoubleConversion < Formula
  desc "Binary-decimal and decimal-binary routines for IEEE doubles"
  homepage "https://github.com/google/double-conversion"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-double-conversion-3.4.0-1-any.pkg.tar.zst"
  sha256 "b3abc4326bc695fe97cf29389513badf65a1ac3dc2841c71a1340edd483af17c"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/google/double-conversion/archive/refs/tags/v3.4.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"double-conversion/ieee.h"
    assert_path_exists lib/"libdouble-conversion.dll.a"
    assert_path_exists bin/"libdouble-conversion.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/double-conversion.pc").read
  end
end
