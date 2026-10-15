class LibgeditGfls < Formula
  desc "Gedit Technology - File loading and saving"
  homepage "https://gitlab.gnome.org/World/gedit/libgedit-gfls"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgedit-gfls-0.4.0-1-any.pkg.tar.zst"
  sha256 "605dd4b51c588ec9b84db9ebade52c09e4b69b65b8fdcc6e12aee0ff9836b540"
  license "LGPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://gitlab.gnome.org/World/gedit/libgedit-gfls/-/archive/0.5.0/libgedit-gfls-0.5.0.tar.bz2"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libgedit-gfls-1/gfls/gfls.h"
    assert_path_exists lib/"libgedit-gfls-1.dll.a"
    assert_path_exists bin/"libgedit-gfls-1-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libgedit-gfls-1.pc").read
  end
end
