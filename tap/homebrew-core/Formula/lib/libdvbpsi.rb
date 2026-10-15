class Libdvbpsi < Formula
  desc "Library to decode/generate MPEG TS and DVB PSI tables"
  homepage "https://www.videolan.org/developers/libdvbpsi.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libdvbpsi-1.3.3-3-any.pkg.tar.zst"
  sha256 "3d94c668e0096164eba4153bb75227434b3ab5157ec3fdd726bba0d4e35681d1"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.videolan.org/pub/libdvbpsi/"
    regex(%r{href=["']?v?(\d+(?:\.\d+)+)/?["' >]}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"dvbpsi/dr.h"
    assert_path_exists lib/"libdvbpsi.dll.a"
    assert_path_exists bin/"libdvbpsi-10.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libdvbpsi.pc").read
  end
end
