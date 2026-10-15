class Libspectre < Formula
  desc "Small library for rendering Postscript documents"
  homepage "https://wiki.freedesktop.org/www/Software/libspectre/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libspectre-0.2.12-2-any.pkg.tar.zst"
  sha256 "25631d04580923d46acb26da3b4280d6ec576e28989f8562e8ba432fb20884fe"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://libspectre.freedesktop.org/releases/"
    regex(/href=.*?libspectre[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libspectre/spectre.h"
    assert_path_exists lib/"libspectre.dll.a"
    assert_path_exists bin/"libspectre-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libspectre.pc").read
  end
end
