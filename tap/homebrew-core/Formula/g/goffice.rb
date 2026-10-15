class Goffice < Formula
  desc "Gnumeric spreadsheet program"
  homepage "https://gitlab.gnome.org/GNOME/goffice"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-goffice-0.10.61-1-any.pkg.tar.zst"
  sha256 "748996563fe84edea7439ccb38b817790b29b1ea2902f28702afb9b44b073996"
  license any_of: ["GPL-3.0-only", "GPL-2.0-only"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/goffice/0.10/goffice-0.10.62.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libgoffice-0.10/goffice/goffice.h"
    assert_path_exists lib/"libgoffice-0.10.dll.a"
    assert_path_exists bin/"libgoffice-0.10-10.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libgoffice-0.10.pc").read
  end
end
