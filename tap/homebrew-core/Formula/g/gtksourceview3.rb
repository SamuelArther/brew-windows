class Gtksourceview3 < Formula
  desc "Text view with syntax, undo/redo, and text marks"
  homepage "https://projects.gnome.org/gtksourceview/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gtksourceview3-3.24.11-7-any.pkg.tar.zst"
  sha256 "94afa142732b32cd8976eb62f97ed540af79f55c6593f82a3733de2f4ada4c93"
  license "LGPL-2.1-or-later"
  revision 5

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/gtksourceview/3.24/gtksourceview-3.24.11.tar.xz"
    regex(/gtksourceview[._-]v?(3\.([0-8]\d*?)?[02468](?:\.\d+)*?)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"gtksourceview-3.0/gtksourceview/gtksource.h"
    assert_path_exists lib/"libgtksourceview-3.0.dll.a"
    assert_path_exists bin/"libgtksourceview-3.0-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/gtksourceview-3.0.pc").read
  end
end
