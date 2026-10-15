class Quazip < Formula
  desc "C++ wrapper over Gilles Vollant's ZIP/UNZIP package"
  homepage "https://github.com/stachenov/quazip/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-quazip-1.7.1-1-any.pkg.tar.zst"
  sha256 "9be58e0dc06afbf655534c7c12534f02605df5dd8175b2aa4cb74ec7cdc72a46"
  license "LGPL-2.1-only"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/stachenov/quazip/archive/refs/tags/v1.7.2.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"QuaZip-Qt6-1.7.1/quazip/zip.h"
    assert_path_exists lib/"libquazip1-qt6.dll.a"
    assert_path_exists bin/"libquazip1-qt6.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/quazip1-qt6.pc").read
  end
end
