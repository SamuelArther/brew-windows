class Libconfini < Formula
  desc "Yet another INI parser"
  homepage "https://madmurphy.github.io/libconfini/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libconfini-1.16.4-1-any.pkg.tar.zst"
  sha256 "636b6e741e5d3e40c1ce0ea697d1ffef1f17038147b9ecf35a7331c247da5651"
  license "GPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/madmurphy/libconfini/releases/download/1.16.4/libconfini-1.16.4-with-configure.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"confini.h"
    assert_path_exists lib/"libconfini.dll.a"
    assert_path_exists bin/"libconfini.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libconfini.pc").read
  end
end
