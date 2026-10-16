class Rtmidi < Formula
  desc "API for realtime MIDI input/output"
  homepage "https://github.com/thestk/rtmidi"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-rtmidi-6.0.0-4-any.pkg.tar.zst"
  sha256 "a1b821cc99e37fb4131331b08aecd9ee54673728affd51adc824673a3cfe7fe3"
  license "MIT"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/thestk/rtmidi/archive/refs/tags/6.0.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"rtmidi/RtMidi.h"
    assert_path_exists lib/"librtmidi.dll.a"
    assert_path_exists bin/"librtmidi-7.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/rtmidi.pc").read
  end
end
