class Sdl12Compat < Formula
  desc "SDL 1.2 compatibility layer that uses SDL 2.0 behind the scenes"
  homepage "https://github.com/libsdl-org/sdl12-compat"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-sdl12-compat-1.2.74-1-any.pkg.tar.zst"
  sha256 "e037b5092f7c071cc4733d8e131a9e5f2fa13454d437511fa673f18e623e1bda"
  license all_of: ["Zlib", "MIT-0"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/libsdl-org/sdl12-compat/archive/refs/tags/release-1.2.78.tar.gz"
    regex(/^release[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"SDL/SDL.h"
    assert_path_exists lib/"libSDL.dll.a"
    assert_path_exists bin/"SDL.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/sdl12_compat.pc").read
  end
end
