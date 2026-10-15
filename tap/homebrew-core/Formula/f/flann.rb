class Flann < Formula
  desc "Fast Library for Approximate Nearest Neighbors"
  homepage "https://github.com/flann-lib/flann"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-flann-1.9.2-2-any.pkg.tar.zst"
  sha256 "a830eb60870d755ee66add38dfcb5e629d39a3bc2dc8b7cf00a46b696024947f"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/flann-lib/flann/archive/refs/tags/1.9.2.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"flann/flann.h"
    assert_path_exists lib/"libflann.dll.a"
    assert_path_exists bin/"libflann.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/flann.pc").read
  end
end
