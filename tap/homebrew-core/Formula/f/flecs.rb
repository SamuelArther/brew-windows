class Flecs < Formula
  desc "Fast entity component system for C & C++"
  homepage "https://www.flecs.dev"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-flecs-4.1.6-1-any.pkg.tar.zst"
  sha256 "ab0e867e5f885eeb8f4ae09c5e0dbf87bcd80b6fdeb20c7e8c42579ae85480d7"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/SanderMertens/flecs/archive/refs/tags/v4.1.6.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"flecs.h"
    assert_path_exists lib/"libflecs.dll.a"
    assert_path_exists bin/"libflecs.dll"
  end
end
