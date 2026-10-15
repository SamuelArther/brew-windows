class Sdl2Compat < Formula
  desc "SDL2 compatibility layer that uses SDL3 behind the scenes"
  homepage "https://github.com/libsdl-org/sdl2-compat"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-sdl2-compat-2.32.74-1-any.pkg.tar.zst"
  sha256 "a6f686586daf3f4994bc428e14fb7cd4c5cf8f05b8bdfcbb116f4817fb61c59f"
  license "Zlib"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/libsdl-org/sdl2-compat/releases/download/release-2.32.74/sdl2-compat-2.32.74.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"SDL2/SDL.h"
    assert_path_exists lib/"libSDL2.dll.a"
    assert_path_exists bin/"SDL2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/sdl2-compat.pc").read
  end
end
