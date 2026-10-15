class Libcbor < Formula
  desc "CBOR protocol implementation for C and others"
  homepage "https://github.com/PJK/libcbor"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libcbor-0.14.0-1-any.pkg.tar.zst"
  sha256 "9b079babf21941b41635728a8bf1c53ab905189c06d97157e84ca8b9c531182d"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/PJK/libcbor/archive/refs/tags/v0.14.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"cbor.h"
    assert_path_exists lib/"libcbor.dll.a"
    assert_path_exists bin/"libcbor.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libcbor.pc").read
  end
end
