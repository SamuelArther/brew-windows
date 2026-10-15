class Libsoundio < Formula
  desc "Cross-platform audio input and output"
  homepage "http://libsound.io"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libsoundio-2.0.1-1-any.pkg.tar.zst"
  sha256 "0d6449ca985cf40ff8253e86b385f176d9bce500df18b294d5dfe08fcd7e115d"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/andrewrk/libsoundio/archive/refs/tags/2.0.1-7.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"soundio/endian.h"
    assert_path_exists lib/"libsoundio.dll.a"
  end
end
