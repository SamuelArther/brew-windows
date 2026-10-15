class Pkcs11Helper < Formula
  desc "Library to simplify the interaction with PKCS#11"
  homepage "https://github.com/OpenSC/OpenSC/wiki/pkcs11-helper"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-pkcs11-helper-1.30.0-2-any.pkg.tar.zst"
  sha256 "9fc867f518b3dc965ad99b339dfd5355dd75168e9980afbff021219d1803c9aa"
  license any_of: ["BSD-3-Clause", "GPL-2.0-or-later"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/OpenSC/pkcs11-helper/releases/download/pkcs11-helper-1.31.0/pkcs11-helper-1.31.0.tar.bz2"
    regex(/pkcs11-helper[._-]v?(\d+(?:\.\d+)+)/i)
    strategy :github_latest
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"pkcs11-helper-1.0/pkcs11.h"
    assert_path_exists lib/"libpkcs11-helper.dll.a"
    assert_path_exists bin/"libpkcs11-helper-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libpkcs11-helper-1.pc").read
  end
end
