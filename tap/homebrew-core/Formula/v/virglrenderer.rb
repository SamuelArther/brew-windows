class Virglrenderer < Formula
  desc "VirGL virtual OpenGL renderer"
  homepage "https://gitlab.freedesktop.org/virgl/virglrenderer/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-virglrenderer-1.3.0-1-any.pkg.tar.zst"
  sha256 "a3fa9f88cb49f769bfc1129276ad2a73bcc2f76972e4bb7e6ad32be2c40eaefb"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://gitlab.freedesktop.org/virgl/virglrenderer/-/archive/1.3.0/virglrenderer-1.3.0.tar.bz2"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"virgl/virgl-version.h"
    assert_path_exists lib/"libvirglrenderer.dll.a"
    assert_path_exists bin/"libvirglrenderer-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/virglrenderer.pc").read
  end
end
