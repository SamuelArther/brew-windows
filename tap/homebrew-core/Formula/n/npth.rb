class Npth < Formula
  desc "New GNU portable threads library"
  homepage "https://gnupg.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-npth-1.8-2-any.pkg.tar.zst"
  sha256 "cc0974ef7bbe32816aa0c7247888f9460e7fcfb91edeca66816b90124e285ef8"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://gnupg.org/ftp/gcrypt/npth/"
    regex(/href=.*?npth[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"npth.h"
    assert_path_exists lib/"libnpth.dll.a"
    assert_path_exists bin/"libnpth-0.dll"
  end
end
