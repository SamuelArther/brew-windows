class Rtaudio < Formula
  desc "API for realtime audio input/output"
  homepage "https://github.com/thestk/rtaudio"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-rtaudio-6.0.1-2-any.pkg.tar.zst"
  sha256 "4e3b16f4bec76cfb79bbf3841ffc4c521eff5571b66c11cc5d06f7d35a4e01ac"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/thestk/rtaudio/archive/refs/tags/6.0.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"rtaudio/RtAudio.h"
    assert_path_exists lib/"librtaudio.dll.a"
    assert_path_exists bin/"librtaudio-7.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/rtaudio.pc").read
  end
end
