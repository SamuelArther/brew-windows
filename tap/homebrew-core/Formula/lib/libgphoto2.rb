class Libgphoto2 < Formula
  desc "Gphoto2 digital camera library"
  homepage "http://www.gphoto.org/proj/libgphoto2/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgphoto2-2.5.34-2-any.pkg.tar.zst"
  sha256 "2657f450ba44205df707b216af7128fdf32daa695e0ec66361a16385a95a61f2"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/gphoto/libgphoto/2.5.34/libgphoto2-2.5.34.tar.bz2"
    regex(%r{url=.*?/libgphoto2[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"gphoto2/gphoto2.h"
    assert_path_exists lib/"libgphoto2.dll.a"
    assert_path_exists bin/"libgphoto2-6.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libgphoto2.pc").read
  end
end
