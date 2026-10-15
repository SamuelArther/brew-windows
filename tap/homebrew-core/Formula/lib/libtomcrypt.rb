class Libtomcrypt < Formula
  desc "Comprehensive, modular and portable cryptographic toolkit"
  homepage "https://www.libtom.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libtomcrypt-1.18.2-5-any.pkg.tar.zst"
  sha256 "0fff21d06556d57a3410d9cd02065bbfb7b2dadd6be1dae4d7dc07f10686b03a"
  license "Unlicense"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/libtom/libtomcrypt/archive/refs/tags/v1.18.2.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"tomcrypt.h"
    assert_path_exists lib/"libtomcrypt.dll.a"
    assert_path_exists bin/"libtomcrypt-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libtomcrypt.pc").read
  end
end
