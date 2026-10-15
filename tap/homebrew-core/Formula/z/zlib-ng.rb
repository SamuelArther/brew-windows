class ZlibNg < Formula
  desc "Zlib replacement with optimizations for next generation systems"
  homepage "https://github.com/zlib-ng/zlib-ng"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-ng-2.3.3-2-any.pkg.tar.zst"
  sha256 "1d7dda169b6833a8f5d6cbbd2c630e2b88165fa47ed3de709b732fa3259023b7"
  license "Zlib"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/zlib-ng/zlib-ng/archive/refs/tags/2.3.3.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"zlib-ng.h"
    assert_path_exists lib/"libz-ng.dll.a"
    assert_path_exists bin/"libz-ng2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/zlib-ng.pc").read
  end
end
