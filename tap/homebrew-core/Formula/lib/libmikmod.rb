class Libmikmod < Formula
  desc "Portable sound library"
  homepage "https://mikmod.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libmikmod-3.3.15-1-any.pkg.tar.zst"
  sha256 "916c0ffe41c61358a33d79eb6f9733eda83e967b6d9ad4fe25abaf498bccd330"
  license "LGPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/mikmod/libmikmod/3.3.15/libmikmod-3.3.15.tar.gz"
    regex(%r{url=.*?/libmikmod[._-](\d+(?:\.\d+)+)\.t}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"mikmod.h"
    assert_path_exists lib/"libmikmod.dll.a"
    assert_path_exists bin/"libmikmod-3.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libmikmod.pc").read
  end
end
