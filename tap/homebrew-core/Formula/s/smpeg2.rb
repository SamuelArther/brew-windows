class Smpeg2 < Formula
  desc "SDL MPEG Player Library"
  homepage "https://icculus.org/smpeg/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-smpeg2-2.0.0-8-any.pkg.tar.zst"
  sha256 "e66075bc199ab3f03a0bc5a6a1323dfc55d607f05f83293399f4b1c719d95e87"
  license "LGPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/icculus/smpeg/archive/refs/tags/release_2_0_0.tar.gz"
    regex(/^release[._-]v?(2(?:[._]\d+)+)$/i)
    strategy :git do |tags, regex|
      tags.map { |tag| tag[regex, 1]&.tr("_", ".") }
    end
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"smpeg2/MPEG.h"
    assert_path_exists lib/"libsmpeg2.dll.a"
    assert_path_exists bin/"smpeg2.dll"
  end
end
