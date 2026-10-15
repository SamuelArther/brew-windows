class Libgit2Glib < Formula
  desc "Glib wrapper library around libgit2 git access library"
  homepage "https://gitlab.gnome.org/GNOME/libgit2-glib"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgit2-glib-1.2.0-8-any.pkg.tar.zst"
  sha256 "a650b2216856ad169a6b0026afed337c39b4ed22490fd61d712ffa683c1102b8"
  license "LGPL-2.1-only"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://gitlab.gnome.org/GNOME/libgit2-glib/-/archive/v1.2.1/libgit2-glib-v1.2.1.tar.bz2"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libgit2-glib-1.0/libgit2-glib/ggit.h"
    assert_path_exists lib/"libgit2-glib-1.0.dll.a"
    assert_path_exists bin/"libgit2-glib-1.0-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libgit2-glib-1.0.pc").read
  end
end
