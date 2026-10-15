class Clipper2 < Formula
  desc "Polygon clipping and offsetting library"
  homepage "https://github.com/AngusJohnson/Clipper2"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-clipper2-2.0.1-1-any.pkg.tar.zst"
  sha256 "9ca30ab553829f95be51a0c21cf07719eca362a65bac2ae2ec59e61053c6be05"
  license "BSL-1.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/AngusJohnson/Clipper2/releases/download/Clipper2_2.0.1/Clipper2_2.0.1.zip"
    regex(/^Clipper2[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"clipper2/clipper.h"
    assert_path_exists lib/"libClipper2.dll.a"
    assert_path_exists bin/"libClipper2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/Clipper2.pc").read
  end
end
