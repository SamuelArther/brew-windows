class Libspelling < Formula
  desc "Spellcheck library for GTK 4"
  homepage "https://gitlab.gnome.org/GNOME/libspelling"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libspelling-0.4.10-1-any.pkg.tar.zst"
  sha256 "8c7596d1a707f9ff78e4241a4e2c5bbd9630154469251585b492f1ea59832fe8"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://gitlab.gnome.org/GNOME/libspelling/-/archive/0.4.10/libspelling-0.4.10.tar.bz2"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libspelling-1/libspelling.h"
    assert_path_exists lib/"libspelling-1.dll.a"
    assert_path_exists bin/"libspelling-1-2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libspelling-1.pc").read
  end
end
