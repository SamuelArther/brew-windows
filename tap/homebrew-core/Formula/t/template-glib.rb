class TemplateGlib < Formula
  desc "GNOME templating library for GLib"
  homepage "https://gitlab.gnome.org/GNOME/template-glib"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-template-glib-3.36.3-2-any.pkg.tar.zst"
  sha256 "2d07e3ab19d875381b84cf457a869eb8b5469752b46419920aafc5e435156a21"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/template-glib/3.40/template-glib-3.40.0.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"template-glib-1.0/tmpl-expr.h"
    assert_path_exists lib/"libtemplate_glib-1.0.dll.a"
    assert_path_exists bin/"libtemplate_glib-1.0-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/template-glib-1.0.pc").read
  end
end
