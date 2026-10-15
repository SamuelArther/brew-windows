class Aribb24 < Formula
  desc "Library for ARIB STD-B24, decoding JIS 8 bit characters and parsing MPEG-TS"
  homepage "https://code.videolan.org/jeeb/aribb24"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aribb24-1.0.3-7-any.pkg.tar.zst"
  sha256 "d7587251ec894c59efcbb08b374a4df618f2db9636fe2418d8a384908506f571"
  license "LGPL-3.0-only"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://code.videolan.org/jeeb/aribb24/-/archive/v1.0.4/aribb24-v1.0.4.tar.bz2"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"aribb24/bits.h"
    assert_path_exists lib/"libaribb24.dll.a"
    assert_path_exists bin/"libaribb24-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/aribb24.pc").read
  end
end
