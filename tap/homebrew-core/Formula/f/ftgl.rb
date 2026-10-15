class Ftgl < Formula
  desc "Freetype / OpenGL bridge"
  homepage "https://sourceforge.net/projects/ftgl/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-ftgl-2.4.0-3-any.pkg.tar.zst"
  sha256 "1fb58077d97b325c6dcfe01173aee7cef040ba6c68aaec7872be43eeead3ac47"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/ftgl/FTGL%20Source/2.1.3~rc5/ftgl-2.1.3-rc5.tar.gz"
    regex(%r{url=.*?/ftgl[._-]v?(\d+(?:\.\d+)+(?:-rc\d*)?)\.t}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"FTGL/ftgl.h"
    assert_path_exists lib/"libftgl.dll.a"
    assert_path_exists bin/"libftgl-2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/ftgl.pc").read
  end
end
