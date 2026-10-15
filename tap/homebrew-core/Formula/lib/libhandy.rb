class Libhandy < Formula
  desc "Building blocks for modern adaptive GNOME apps"
  homepage "https://gitlab.gnome.org/GNOME/libhandy"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libhandy-1.8.3-2-any.pkg.tar.zst"
  sha256 "fcead9cbc2f4cc94dc4938cbb2f49bac966c8a237a274ef1cde1a1d34e72b9bb"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://gitlab.gnome.org/GNOME/libhandy/-/archive/1.8.3/libhandy-1.8.3.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libhandy-1/handy.h"
    assert_path_exists lib/"libhandy-1.dll.a"
    assert_path_exists bin/"libhandy-1-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libhandy-1.pc").read
  end
end
