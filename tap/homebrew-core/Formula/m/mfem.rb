class Mfem < Formula
  desc "Free, lightweight, scalable C++ library for FEM"
  homepage "https://mfem.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-mfem-4.9-3-any.pkg.tar.zst"
  sha256 "2f96114b89ed037095ea2f686e71b792e339d7f6cbab2c98ed2277190cf36cf5"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/mfem/mfem/archive/refs/tags/v4.10.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"mfem.hpp"
    assert_path_exists lib/"libmfem.dll.a"
    assert_path_exists bin/"libmfem.dll"
  end
end
