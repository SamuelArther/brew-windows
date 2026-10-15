class Freeglut < Formula
  desc "Open-source alternative to the OpenGL Utility Toolkit (GLUT) library"
  homepage "https://freeglut.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-freeglut-3.8.0-1-any.pkg.tar.zst"
  sha256 "b9b9212f0b7e5ba42709e153143efac410765bde9fd1ad9991bc025bf1567805"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/freeglut/freeglut/releases/download/v3.8.0/freeglut-3.8.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"GL/glut.h"
    assert_path_exists lib/"libfreeglut.dll.a"
    assert_path_exists bin/"libfreeglut.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/freeglut.pc").read
  end
end
