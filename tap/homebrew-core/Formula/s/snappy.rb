class Snappy < Formula
  desc "Compression/decompression library aiming for high speed"
  homepage "https://google.github.io/snappy/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-snappy-1.3.1-1-any.pkg.tar.zst"
  sha256 "a4037212ff1e8a4c12af0b7b5443d3abb46ca7e30849a45160db90add74b54c8"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/google/snappy/archive/refs/tags/1.3.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"snappy.h"
    assert_path_exists lib/"libsnappy.dll.a"
    assert_path_exists bin/"libsnappy.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/snappy.pc").read
  end
end
