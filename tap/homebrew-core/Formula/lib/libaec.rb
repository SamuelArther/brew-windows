class Libaec < Formula
  desc "Adaptive Entropy Coding implementing Golomb-Rice algorithm"
  homepage "https://github.com/Deutsches-Klimarechenzentrum/libaec"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libaec-1.1.7-2-any.pkg.tar.zst"
  sha256 "b30e0fce8bff0f782e18e216298bf8f27d9dc3f5cf49b7f319b4e8a359658572"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/Deutsches-Klimarechenzentrum/libaec/releases/download/v1.1.7/libaec-1.1.7.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"szlib.h"
    assert_path_exists lib/"libaec.dll.a"
    assert_path_exists bin/"libaec-0.dll"
  end
end
