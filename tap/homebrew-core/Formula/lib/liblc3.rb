class Liblc3 < Formula
  desc "Low Complexity Communication Codec library and tools"
  homepage "https://github.com/google/liblc3"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-liblc3-1.1.3-1-any.pkg.tar.zst"
  sha256 "e97b0b2f9d7b9e5452f883e01cbad504624f4fb036fe889ddd3ba6cec254c5aa"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/google/liblc3/archive/refs/tags/v1.1.3.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"lc3.h"
    assert_path_exists lib/"liblc3.dll.a"
    assert_path_exists bin/"liblc3-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/lc3.pc").read
  end
end
