class M4ri < Formula
  desc "Library for fast arithmetic with dense matrices over GF(2)"
  homepage "https://github.com/malb/m4ri"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-m4ri-20260122-1-any.pkg.tar.zst"
  version "20260122"
  sha256 "f8975afb0ad9073d7e0e545a9a48e8f46b944167af955a7ea9d5a368f2e98639"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/malb/m4ri/releases/download/20260122/m4ri-20260122.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"m4ri/io.h"
    assert_path_exists lib/"libm4ri.dll.a"
    assert_path_exists bin/"libm4ri-2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/m4ri.pc").read
  end
end
