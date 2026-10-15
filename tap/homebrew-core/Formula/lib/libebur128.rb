class Libebur128 < Formula
  desc "Library implementing the EBU R128 loudness standard"
  homepage "https://github.com/jiixyj/libebur128"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libebur128-1.2.6-2-any.pkg.tar.zst"
  sha256 "e51249185e2db0e1d21d2eb99590d7bcc6c724f7ab86a473e5082a30d45fc0b0"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/jiixyj/libebur128/archive/refs/tags/v1.2.6.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"ebur128.h"
    assert_path_exists lib/"libebur128.dll.a"
    assert_path_exists bin/"libebur128.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libebur128.pc").read
  end
end
