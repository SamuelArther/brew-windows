class Sfcgal < Formula
  desc "C++ wrapper library around CGAL"
  homepage "https://sfcgal.gitlab.io/SFCGAL/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-sfcgal-2.3.0-1-any.pkg.tar.zst"
  sha256 "e9d81728af954b39416391342995846ca97b4cd7bbe9dbfeda26e242e306229f"
  license "LGPL-2.0-or-later"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://gitlab.com/sfcgal/SFCGAL/-/archive/v2.3.0/SFCGAL-v2.3.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"SFCGAL/Curve.h"
    assert_path_exists lib/"libSFCGAL.dll.a"
    assert_path_exists bin/"libSFCGAL.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/sfcgal.pc").read
  end
end
