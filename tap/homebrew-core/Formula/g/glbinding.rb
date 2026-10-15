class Glbinding < Formula
  desc "C++ binding for the OpenGL API"
  homepage "https://glbinding.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-glbinding-3.5.0-2-any.pkg.tar.zst"
  sha256 "2f348504e117f4da803b487ec00bfc106adb2ca5fa85455d0f878ee0775b737e"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/cginternals/glbinding/archive/refs/tags/v3.5.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"glbinding/nogl.h"
    assert_path_exists lib/"libglbinding-aux.dll.a"
    assert_path_exists bin/"libglbinding-3.dll"
  end
end
