class Libshumate < Formula
  desc "Shumate is a GTK toolkit providing widgets for embedded maps"
  homepage "https://gitlab.gnome.org/GNOME/libshumate"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libshumate-1.7.0-1-any.pkg.tar.zst"
  sha256 "d0c4e778c6364c0b765f93b17886f48655bacdaf94912bb777a60001d368cd39"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/libshumate/1.7/libshumate-1.7.0.tar.xz"
    regex(/libshumate[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"shumate-1.0/shumate/shumate.h"
    assert_path_exists lib/"libshumate-1.0.dll.a"
    assert_path_exists bin/"libshumate-1.0-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/shumate-1.0.pc").read
  end
end
