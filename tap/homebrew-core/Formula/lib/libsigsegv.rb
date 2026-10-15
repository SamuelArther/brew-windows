class Libsigsegv < Formula
  desc "Library for handling page faults in user mode"
  homepage "https://www.gnu.org/software/libsigsegv/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libsigsegv-2.13-2-any.pkg.tar.zst"
  sha256 "28ae6870eb72de6ba07859b310e1f8715bd0ffb67784c86fd0aa7cc2d6456011"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://ftpmirror.gnu.org/libsigsegv/libsigsegv-2.15.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"sigsegv.h"
    assert_path_exists lib/"libsigsegv.dll.a"
    assert_path_exists bin/"libsigsegv-2.dll"
  end
end
