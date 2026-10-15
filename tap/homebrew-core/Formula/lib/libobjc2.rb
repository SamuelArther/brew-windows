class Libobjc2 < Formula
  desc "Objective-C runtime library intended for use with Clang"
  homepage "https://github.com/gnustep/libobjc2"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libobjc2-2.2.1-7-any.pkg.tar.zst"
  sha256 "d38f9ea87f986a58355f4b6ba8e9ecfe751bca5c190905789dd8f6a0a186a2de"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/gnustep/libobjc2/archive/refs/tags/v2.3.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"Block.h"
    assert_path_exists lib/"libobjc.dll.a"
    assert_path_exists bin/"libobjc-4.6.dll"
  end
end
