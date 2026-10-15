class Embree < Formula
  desc "High-performance ray tracing kernels"
  homepage "https://www.embree.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-embree-4.4.1-3-any.pkg.tar.zst"
  sha256 "ab736f96e8cc0e9dd0f85925df49991701301791cf1ff89562be4f81f0907737"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/RenderKit/embree/archive/refs/tags/v4.4.1.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"embree4/rtcore.h"
    assert_path_exists lib/"libembree4.dll.a"
    assert_path_exists bin/"libembree4.dll"
  end
end
