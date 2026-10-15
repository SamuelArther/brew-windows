class PangommAT246 < Formula
  desc "C++ interface to Pango"
  homepage "https://www.gtk.org/docs/architecture/pango"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-pangomm-2.46.4-2-any.pkg.tar.zst"
  sha256 "d211e2e00796b79ff99af1888f16ac6d9696afc6c7ba08ff9027c5c418365a70"
  license "LGPL-2.1-only"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/pangomm/2.46/pangomm-2.46.5.tar.xz"
    regex(/pangomm-(2\.46(?:\.\d+)*)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"pangomm-1.4/pangomm.h"
    assert_path_exists lib/"libpangomm-1.4.dll.a"
    assert_path_exists bin/"libpangomm-1.4-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/pangomm-1.4.pc").read
  end
end
