class Isl < Formula
  desc "Integer Set Library for the polyhedral model"
  homepage "https://libisl.sourceforge.io/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-isl-0.28-1-any.pkg.tar.zst"
  sha256 "8594e01a253d5a72646c586cf5a1626c1b1c2be7c30060fe67abd9bebe3fe223"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url :homepage
    regex(/href=.*?isl[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"isl/id.h"
    assert_path_exists lib/"libisl.dll.a"
    assert_path_exists bin/"libisl-23.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/isl.pc").read
  end
end
