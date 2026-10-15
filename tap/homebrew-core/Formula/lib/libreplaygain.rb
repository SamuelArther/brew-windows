class Libreplaygain < Formula
  desc "Library to implement ReplayGain standard for audio"
  homepage "https://www.musepack.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libreplaygain-475-3-any.pkg.tar.zst"
  version "475"
  sha256 "9e84af96e870cf42505ea23d06a7f3827250aaceb41159428031cbc9185b55c2"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://www.musepack.net/index.php?pg=src"
    regex(/href=.*?libreplaygain[._-](r\d+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"replaygain/gain_analysis.h"
    assert_path_exists lib/"libreplaygain.dll.a"
    assert_path_exists bin/"libreplaygain-1.dll"
  end
end
