class MinizipNg < Formula
  desc "Zip file manipulation library with minizip 1.x compatibility layer"
  homepage "https://github.com/zlib-ng/minizip-ng"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-minizip-ng-4.2.0-1-any.pkg.tar.zst"
  sha256 "c447aa64dd15b0e542d873b4dbfb11fd501815609faf24649d96f6757f5b678e"
  license "Zlib"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/zlib-ng/minizip-ng/archive/refs/tags/4.2.2.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"minizip-ng/mz.h"
    assert_path_exists lib/"libminizip-ng.dll.a"
    assert_path_exists bin/"libminizip-ng-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/minizip-ng.pc").read
  end
end
