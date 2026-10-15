class Libthai < Formula
  desc "Thai language support library"
  homepage "https://linux.thai.net/projects/libthai"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libthai-0.1.30-1-any.pkg.tar.zst"
  sha256 "c90ebbca3e7b4c34f4b3ffa501ba8d00cdaf6340cff7db3c46d79d5e01ef207c"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://linux.thai.net/pub/thailinux/software/libthai/"
    regex(/href=["']?libthai[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"thai/tis.h"
    assert_path_exists lib/"libthai.dll.a"
    assert_path_exists bin/"libthai-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libthai.pc").read
  end
end
