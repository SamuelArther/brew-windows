class Libb2 < Formula
  desc "Secure hashing function"
  homepage "https://blake2.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libb2-0.98.1-3-any.pkg.tar.zst"
  sha256 "dfb554635119d3c46adb00c9d0c589bb38d1cb0781b10ecf31de07ce388bd0c0"
  license "CC0-1.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/BLAKE2/libb2/releases/download/v0.98.1/libb2-0.98.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"blake2.h"
    assert_path_exists lib/"libb2.dll.a"
    assert_path_exists bin/"libb2-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libb2.pc").read
  end
end
