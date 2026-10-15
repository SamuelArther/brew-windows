class Pcaudiolib < Formula
  desc "Portable C Audio Library"
  homepage "https://github.com/espeak-ng/pcaudiolib"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-pcaudiolib-1.3-1-any.pkg.tar.zst"
  sha256 "6afea64b060093e45bfca9f6c917f64b531eb161f682adf9f5ab666cb008abae"
  license "GPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/espeak-ng/pcaudiolib/releases/download/1.3/pcaudiolib-1.3.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"pcaudiolib/audio.h"
    assert_path_exists lib/"libpcaudio.dll.a"
    assert_path_exists bin/"libpcaudio-0.dll"
  end
end
