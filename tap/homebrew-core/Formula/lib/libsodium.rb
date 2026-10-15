class Libsodium < Formula
  desc "NaCl networking and cryptography library"
  homepage "https://libsodium.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libsodium-1.0.22-3-any.pkg.tar.zst"
  sha256 "a221c0ea96129cb5ece1a9a718a9a1f5cfc9dd427c075d07a997cb11c8de32d9"
  license "ISC"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.libsodium.org/libsodium/releases/"
    regex(/href=.*?libsodium[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"sodium.h"
    assert_path_exists lib/"libsodium.dll.a"
    assert_path_exists bin/"libsodium-26.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libsodium.pc").read
  end
end
