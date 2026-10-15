class Mimalloc < Formula
  desc "Compact general purpose allocator"
  homepage "https://github.com/microsoft/mimalloc"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-mimalloc-3.5.3-1-any.pkg.tar.zst"
  sha256 "bbfdf4b113c31ed9c669ade72c861ee21ad89cfdad4324452ae8e15e75c95756"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/microsoft/mimalloc/archive/refs/tags/v3.5.4.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"mimalloc.h"
    assert_path_exists lib/"libmimalloc.dll.a"
    assert_path_exists bin/"libmimalloc.dll"
  end
end
