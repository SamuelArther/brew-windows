class LibsigcxxAT2 < Formula
  desc "Callback framework for C++"
  homepage "https://libsigcplusplus.github.io/libsigcplusplus/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libsigc++-2.12.2-1-any.pkg.tar.zst"
  sha256 "1d98e6609c2b700bf83f7ea447f4eb9f3bea279e9fc217bac5ac12c371a13d33"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/libsigcplusplus/libsigcplusplus/releases/download/2.12.2/libsigc++-2.12.2.tar.xz"
    regex(/^v?(2\.([0-8]\d*?)?[02468](?:\.\d+)*?)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"sigc++-2.0/sigc++/bind.h"
    assert_path_exists lib/"libsigc-2.0.dll.a"
    assert_path_exists bin/"libsigc-2.0-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/sigc++-2.0.pc").read
  end
end
