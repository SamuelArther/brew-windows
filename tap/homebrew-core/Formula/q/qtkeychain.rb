class Qtkeychain < Formula
  desc "Platform-independent Qt API for storing passwords securely"
  homepage "https://github.com/frankosterfeld/qtkeychain"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-qtkeychain-0.17.0-1-any.pkg.tar.zst"
  sha256 "6aadfda138baa1d2d10f5c8163eb6abe51dfae3325787bedb27c73693f4ebd2d"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/frankosterfeld/qtkeychain/archive/refs/tags/0.17.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"qt6keychain/keychain.h"
    assert_path_exists lib/"libqt6keychain.dll.a"
    assert_path_exists bin/"libqt6keychain.dll"
  end
end
