class Libcerf < Formula
  desc "Numeric library for complex error functions"
  homepage "https://jugit.fz-juelich.de/mlz/lib/cerf"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libcerf-1~3.3-1-any.pkg.tar.zst"
  version "3.3"
  sha256 "cd0e76994e69d7579aa55828fd5a7c12bf8f900ae585a80c5e228c934c98d9ff"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://jugit.fz-juelich.de/api/v4/projects/269/releases"
    regex(/libcerf[._-]v?((?!2\.0)\d+(?:\.\d+)+)/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"cerf.h"
    assert_path_exists lib/"libcerf.dll.a"
    assert_path_exists bin/"libcerf-3.dll"
  end
end
