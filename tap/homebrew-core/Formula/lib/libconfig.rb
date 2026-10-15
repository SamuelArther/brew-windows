class Libconfig < Formula
  desc "Configuration file processing library"
  homepage "https://hyperrealm.github.io/libconfig/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libconfig-1.8.2-1-any.pkg.tar.zst"
  sha256 "cad08bce9e79ed553a81b3b2253fb1ba2c8bb287f9b447b4ee8e9cbba3cb87b5"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/hyperrealm/libconfig/archive/refs/tags/v1.8.2.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libconfig.h"
    assert_path_exists lib/"libconfig++.dll.a"
    assert_path_exists bin/"libconfig++-15.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libconfig++.pc").read
  end
end
