class Libmicrohttpd < Formula
  desc "Light HTTP/1.1 server library"
  homepage "https://www.gnu.org/software/libmicrohttpd/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libmicrohttpd-1.0.10-1-any.pkg.tar.zst"
  sha256 "306d11dd2758290f4d0dbc66837a230308056acbc63b1b8fb1d7f66de2a21060"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://ftpmirror.gnu.org/libmicrohttpd/libmicrohttpd-1.0.10.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"microhttpd.h"
    assert_path_exists lib/"libmicrohttpd.dll.a"
    assert_path_exists bin/"libmicrohttpd-12.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libmicrohttpd.pc").read
  end
end
