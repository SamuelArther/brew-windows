class Portmidi < Formula
  desc "Cross-platform library for real-time MIDI I/O"
  homepage "https://github.com/PortMidi/portmidi"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-portmidi-1~2.0.8-1-any.pkg.tar.zst"
  version "2.0.8"
  sha256 "7e5eb4f1e7971aa88cbfa629c9d83f072c32ebf56eee9712b89efa717fe0f74b"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/PortMidi/portmidi/archive/refs/tags/v2.0.8.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"pmutil.h"
    assert_path_exists lib/"libportmidi.dll.a"
    assert_path_exists bin/"libportmidi.dll"
  end
end
