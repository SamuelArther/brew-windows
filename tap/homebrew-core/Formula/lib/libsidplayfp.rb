class Libsidplayfp < Formula
  desc "Library to play Commodore 64 music"
  homepage "https://github.com/libsidplayfp/libsidplayfp"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libsidplayfp-2.16.1-1-any.pkg.tar.zst"
  sha256 "7f394d592f0b40f7ede2c615681b55535a76f5b3eaf6e97cf406c4d1ecdd77e9"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/libsidplayfp/libsidplayfp/releases/download/v3.1.1/libsidplayfp-3.1.1.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"stilview/stil.h"
    assert_path_exists lib/"libsidplayfp.dll.a"
    assert_path_exists bin/"libsidplayfp-6.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libsidplayfp.pc").read
  end
end
