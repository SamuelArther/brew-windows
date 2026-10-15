class Blis < Formula
  desc "BLAS-like Library Instantiation Software Framework"
  homepage "https://github.com/flame/blis"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-blis-2.1-1-any.pkg.tar.zst"
  sha256 "94fb947fae20deedd208ecf23ca5a8f8921d465e2ae4d54cdfaf442d3ee7b2b3"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/flame/blis/archive/refs/tags/2.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"blis.h"
    assert_path_exists lib/"libblis.dll.a"
    assert_path_exists bin/"libblis.4.dll"
  end
end
