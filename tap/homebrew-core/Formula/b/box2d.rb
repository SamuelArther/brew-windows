class Box2d < Formula
  desc "2D physics engine for games"
  homepage "https://box2d.org"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-box2d-3.1.1-1-any.pkg.tar.zst"
  sha256 "75d5cf499fc029070ee402e2ca487d09c6f5cee508c058c76917ee1be699a87d"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/erincatto/box2d/archive/refs/tags/v3.1.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"box2d/id.h"
    assert_path_exists lib/"libbox2d.dll.a"
    assert_path_exists bin/"libbox2d.dll"
  end
end
