class Hiredis < Formula
  desc "Minimalistic client for Redis"
  homepage "https://github.com/redis/hiredis"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-hiredis-1.3.0-2-any.pkg.tar.zst"
  sha256 "e53e5320c4a375f1c7591fe70a698ca00118a09e0850b6ffb8e577fccb938da4"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/redis/hiredis/archive/refs/tags/v1.4.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"hiredis/sds.h"
    assert_path_exists lib/"libhiredis.dll.a"
    assert_path_exists bin/"libhiredis.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/hiredis.pc").read
  end
end
