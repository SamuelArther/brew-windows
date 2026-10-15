class Superlu < Formula
  desc "Solve large, sparse nonsymmetric systems of equations"
  homepage "https://portal.nersc.gov/project/sparse/superlu/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-superlu-7.0.1-1-any.pkg.tar.zst"
  sha256 "2859dcdd7e297ff501434c2bff1a38bdfc22bb47d0a4877e47eb234ee1e0ebe2"
  license "BSD-3-Clause-LBNL"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/xiaoyeli/superlu/archive/refs/tags/v7.0.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"superlu/slu_util.h"
    assert_path_exists lib/"libsuperlu.dll.a"
    assert_path_exists bin/"libsuperlu-7.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/superlu.pc").read
  end
end
