class Libva < Formula
  desc "Hardware accelerated video processing library"
  homepage "https://github.com/intel/libva"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libva-2.24.1-1-any.pkg.tar.zst"
  sha256 "bd3c2aabe37534f17aaa11a5d846034b56b6890767abcbfb2e8c3fd9153a1a6f"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/intel/libva/releases/download/2.24.1/libva-2.24.1.tar.bz2"
    strategy :github_latest
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"va/va.h"
    assert_path_exists lib/"libva.dll.a"
    assert_path_exists bin/"libva.dll"
  end
end
