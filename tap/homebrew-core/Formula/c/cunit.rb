class Cunit < Formula
  desc "Lightweight unit testing framework for C"
  homepage "https://cunit.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cunit-2.1.3-4-any.pkg.tar.zst"
  sha256 "b63fda63d0daca8ca532da5390ae90d569aaa76ecb959776b9d86cfe1bf8e233"
  license "LGPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/cunit/CUnit/2.1-3/CUnit-2.1-3.tar.bz2"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"CUnit/Util.h"
    assert_path_exists lib/"libcunit.dll.a"
    assert_path_exists bin/"libcunit-1.dll"
  end
end
