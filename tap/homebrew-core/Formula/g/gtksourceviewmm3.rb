class Gtksourceviewmm3 < Formula
  desc "C++ bindings for gtksourceview3"
  homepage "https://gitlab.gnome.org/GNOME/gtksourceviewmm"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gtksourceviewmm3-3.21.3-4-any.pkg.tar.zst"
  sha256 "18b7611799aa8f3589f4fc43295de8024776604bb0510944660593d43e8ccdea"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/gtksourceviewmm/3.18/gtksourceviewmm-3.18.0.tar.xz"
    regex(/gtksourceviewmm[._-]v?(3\.([0-8]\d*?)?[02468](?:\.\d+)*?)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"gtksourceviewmm-3.0/gtksourceviewmm.h"
    assert_path_exists lib/"libgtksourceviewmm-3.0.dll.a"
    assert_path_exists bin/"libgtksourceviewmm-3.0-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/gtksourceviewmm-3.0.pc").read
  end
end
