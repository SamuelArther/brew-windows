class Libmypaint < Formula
  desc "MyPaint brush engine library"
  homepage "https://github.com/mypaint/libmypaint/wiki"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libmypaint-1.6.1-5-any.pkg.tar.zst"
  sha256 "d57648965eaba901deee3bd596c150162085b396ced6530bedcff836897bd258"
  license "ISC"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/mypaint/libmypaint/releases/download/v1.6.1/libmypaint-1.6.1.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libmypaint/mypaint-brush.h"
    assert_path_exists lib/"libmypaint.dll.a"
    assert_path_exists bin/"libmypaint-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libmypaint.pc").read
  end
end
