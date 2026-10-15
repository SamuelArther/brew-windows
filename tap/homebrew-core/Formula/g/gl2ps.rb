class Gl2ps < Formula
  desc "OpenGL to PostScript printing library"
  homepage "https://www.geuz.org/gl2ps/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gl2ps-1.4.3-1-any.pkg.tar.zst"
  sha256 "a068cb0df19689e1321a22aae371e7e2464a1dcb2bf18dff98338e99d4db6b94"
  license "GL2PS"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://geuz.org/gl2ps/src/"
    regex(/href=.*?gl2ps[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"gl2ps.h"
    assert_path_exists lib/"libgl2ps.dll.a"
    assert_path_exists bin/"libgl2ps.dll"
  end
end
