class LibgeditTepl < Formula
  desc "Gedit Technology - Text editor product line"
  homepage "https://gitlab.gnome.org/World/gedit/libgedit-tepl"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgedit-tepl-6.14.0-2-any.pkg.tar.zst"
  sha256 "eb5af1bef703b932a3a77649ec1de8de273906c1917009d20ef83a492701238a"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://gitlab.gnome.org/World/gedit/libgedit-tepl/-/archive/6.15.0/libgedit-tepl-6.15.0.tar.bz2"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libgedit-tepl-6/tepl/tepl.h"
    assert_path_exists lib/"libgedit-tepl-6.dll.a"
    assert_path_exists bin/"libgedit-tepl-6-4.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libgedit-tepl-6.pc").read
  end
end
