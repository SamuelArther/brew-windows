class Bullet < Formula
  desc "Physics SDK"
  homepage "https://pybullet.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-bullet-3.25-11-any.pkg.tar.zst"
  sha256 "f55b15b6015b3262a88bcb44e28195135072c8e18499cb4043c85f5ff853b3ac"
  license "Zlib"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/bulletphysics/bullet3/archive/refs/tags/3.25.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"Host.h"
    assert_path_exists lib/"libBullet2FileLoader.dll.a"
    assert_path_exists bin/"libBullet2FileLoader.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/bullet.pc").read
  end
end
