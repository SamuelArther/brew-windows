class Gtksourceview5 < Formula
  desc "Text view with syntax, undo/redo, and text marks"
  homepage "https://projects.gnome.org/gtksourceview/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gtksourceview5-5.22.0-1-any.pkg.tar.zst"
  sha256 "f9a033bfd71b46926f83a96ec60267a1305d08b300a96918788cb72702a60f3b"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/gtksourceview/5.22/gtksourceview-5.22.0.tar.xz"
    regex(/gtksourceview[._-]v?(5\.([0-8]\d*?)?[02468](?:\.\d+)*?)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"gtksourceview-5/gtksourceview/gtksource.h"
    assert_path_exists lib/"libgtksourceview-5.dll.a"
    assert_path_exists bin/"libgtksourceview-5-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/gtksourceview-5.pc").read
  end
end
