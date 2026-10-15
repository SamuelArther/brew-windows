class Libp11 < Formula
  desc "PKCS#11 wrapper library in C"
  homepage "https://github.com/OpenSC/libp11/wiki"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libp11-0.4.18-1-any.pkg.tar.zst"
  sha256 "94bbaf9abb384dd65f18e5a26825fad4fad7b1515ef08c299f4b4b2fe08fcf18"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/OpenSC/libp11/releases/download/libp11-0.4.21/libp11-0.4.21.tar.gz"
    regex(/^libp11[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libp11.h"
    assert_path_exists lib/"libp11.dll.a"
    assert_path_exists bin/"libp11-3.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libp11.pc").read
  end
end
