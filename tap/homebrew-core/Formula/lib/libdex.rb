class Libdex < Formula
  desc "Future-based programming for GLib-based applications"
  homepage "https://gitlab.gnome.org/GNOME/libdex"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libdex-0.8.1-3-any.pkg.tar.zst"
  sha256 "21caf03f0374b83d6bd1f6cd2908c75b11ef0a331953188cf5adcaad28b75874"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://gitlab.gnome.org/GNOME/libdex/-/archive/1.2.0/libdex-1.2.0.tar.gz"
    regex(/^v?(\d+\.\d*[02468](?:\.\d+)*)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libdex-1/libdex.h"
    assert_path_exists lib/"libdex-1.dll.a"
    assert_path_exists bin/"libdex-1-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libdex-1.pc").read
  end
end
