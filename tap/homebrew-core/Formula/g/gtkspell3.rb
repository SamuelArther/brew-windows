class Gtkspell3 < Formula
  desc "Gtk widget for highlighting and replacing misspelled words"
  homepage "https://gtkspell.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gtkspell3-3.0.10-4-any.pkg.tar.zst"
  sha256 "bdc6baeb52bb4a3fc795c0a51d8be38b018d3e9b90f31425eadf5ad0202f566e"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/gtkspell/3.0.10/gtkspell3-3.0.10.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"gtkspell-3.0/gtkspell/gtkspell.h"
    assert_path_exists lib/"libgtkspell3-3.dll.a"
    assert_path_exists bin/"libgtkspell3-3-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/gtkspell3-3.0.pc").read
  end
end
