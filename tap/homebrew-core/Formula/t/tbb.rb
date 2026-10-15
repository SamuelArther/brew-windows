class Tbb < Formula
  desc "Rich and complete approach to parallelism in C++"
  homepage "https://uxlfoundation.github.io/oneTBB/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tbb-2023.1.0-1-any.pkg.tar.zst"
  sha256 "1dcd85163ef30c1ce313d72a9f8df90188da2a1b74effa5bb291250d3f7b0f67"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/uxlfoundation/oneTBB/archive/refs/tags/v2023.1.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"tbb/tbb.h"
    assert_path_exists lib/"libtbb12.dll.a"
    assert_path_exists bin/"libtbb12.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/tbb.pc").read
  end
end
