class Libepoxy < Formula
  desc "Library for handling OpenGL function pointer management"
  homepage "https://github.com/anholt/libepoxy"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libepoxy-1.5.10-7-any.pkg.tar.zst"
  sha256 "c6c6994e8ec49071f06cc1fae475406bdf17603d4687ad0bcac2e11078cfd7c8"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/libepoxy/1.5/libepoxy-1.5.10.tar.xz"
    regex(/libepoxy[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"epoxy/gl.h"
    assert_path_exists lib/"libepoxy.dll.a"
    assert_path_exists bin/"libepoxy-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/epoxy.pc").read
  end
end
