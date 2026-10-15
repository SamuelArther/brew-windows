class Libgee < Formula
  desc "Collection library providing GObject-based interfaces"
  homepage "https://gitlab.gnome.org/GNOME/libgee"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgee-0.20.8-2-any.pkg.tar.zst"
  sha256 "3ab12e0bf5b6406336a1a0bf2028a3ae9929b173cc5c10aadc746d1ef1f4ae07"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/libgee/0.20/libgee-0.20.8.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"gee-0.8/gee.h"
    assert_path_exists lib/"libgee-0.8.dll.a"
    assert_path_exists bin/"libgee-0.8-2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/gee-0.8.pc").read
  end
end
