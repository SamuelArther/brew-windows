class Gtksourceview4 < Formula
  desc "Text view with syntax, undo/redo, and text marks"
  homepage "https://projects.gnome.org/gtksourceview/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gtksourceview4-4.8.4-3-any.pkg.tar.zst"
  sha256 "119e31bbb40060c18c0be86cd3a37d7f1c3a0481897b1f4fdbc070da24086ef9"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/gtksourceview/4.8/gtksourceview-4.8.4.tar.xz"
    regex(/gtksourceview[._-]v?(4\.([0-8]\d*?)?[02468](?:\.\d+)*?)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"gtksourceview-4/gtksourceview/gtksource.h"
    assert_path_exists lib/"libgtksourceview-4.dll.a"
    assert_path_exists bin/"libgtksourceview-4-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/gtksourceview-4.pc").read
  end
end
