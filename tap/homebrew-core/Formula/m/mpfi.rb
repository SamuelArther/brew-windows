class Mpfi < Formula
  desc "Multiple precision interval arithmetic library"
  homepage "https://perso.ens-lyon.fr/nathalie.revol/software.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-mpfi-1.5.4-2-any.pkg.tar.zst"
  sha256 "a7e304112f48e43d439258bfa2654558ac7c9400d45c3c0d766c7a3946ed03e9"
  license all_of: ["GPL-3.0-or-later", "LGPL-2.1-or-later"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url :homepage
    regex(/href=.*?mpfi[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"mpfi.h"
    assert_path_exists lib/"libmpfi.dll.a"
    assert_path_exists bin/"libmpfi-0.dll"
  end
end
