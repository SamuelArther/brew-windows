class Sdl3 < Formula
  desc "Low-level access to audio, keyboard, mouse, joystick, and graphics"
  homepage "https://libsdl.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-sdl3-3.4.18-1-any.pkg.tar.zst"
  sha256 "f9f8e2fe9858868e7653b24c973bcc8c19dea1096afc8c905bf1cd1a87960442"
  license "Zlib"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/libsdl-org/SDL/releases/download/release-3.4.18/SDL3-3.4.18.tar.gz"
    regex(/release[._-](\d+(?:\.\d+)+)/i)
    strategy :github_latest
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"SDL3/SDL.h"
    assert_path_exists lib/"libSDL3.dll.a"
    assert_path_exists bin/"SDL3.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/sdl3.pc").read
  end
end
