class AtkmmAT228 < Formula
  desc "Official C++ interface for the ATK accessibility toolkit library"
  homepage "https://www.gtkmm.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-atkmm-2.28.5-1-any.pkg.tar.zst"
  sha256 "1dd925a3fb016f4264a7f02ba3c1a4565e8e9173e31e9e4cc1d50a0ab32bb8b0"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/atkmm/2.28/atkmm-2.28.5.tar.xz"
    regex(/atkmm-(2\.28(?:\.\d+)*)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"atkmm-1.6/atkmm.h"
    assert_path_exists lib/"libatkmm-1.6.dll.a"
    assert_path_exists bin/"libatkmm-1.6-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/atkmm-1.6.pc").read
  end
end
