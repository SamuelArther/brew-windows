class CairommAT114 < Formula
  desc "Vector graphics library with cross-device output support"
  homepage "https://cairographics.org/cairomm/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cairomm-1.14.6-1-any.pkg.tar.zst"
  sha256 "39d532f40de7010da8947c223dd2f4c8502fc9b6cfabff3132953b749378b5de"
  license "LGPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://cairographics.org/releases/?C=M&O=D"
    regex(/href=.*?cairomm[._-]v?(1\.14(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"cairomm-1.0/cairomm/path.h"
    assert_path_exists lib/"libcairomm-1.0.dll.a"
    assert_path_exists bin/"libcairomm-1.0-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/cairomm-1.0.pc").read
  end
end
