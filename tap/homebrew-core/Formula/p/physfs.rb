class Physfs < Formula
  desc "Library to provide abstract access to various archives"
  homepage "https://icculus.org/physfs/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-physfs-3.2.0-1-any.pkg.tar.zst"
  sha256 "0571ba88d22cc5d1334db0810a911adf9c76272d9a66be0ddfed0676613f5387"
  license "Zlib"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/icculus/physfs/archive/refs/tags/release-3.2.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"physfs.h"
    assert_path_exists lib/"libphysfs.dll.a"
    assert_path_exists bin/"libphysfs.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/physfs.pc").read
  end
end
