class Libsamplerate < Formula
  desc "Library for sample rate conversion of audio data"
  homepage "https://github.com/libsndfile/libsamplerate"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libsamplerate-0.2.2-1-any.pkg.tar.zst"
  sha256 "05a678fb77cc8787e4073907b36357dd985fe06788adc8cc3216a851ee243bf8"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/libsndfile/libsamplerate/releases/download/0.2.2/libsamplerate-0.2.2.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"samplerate.h"
    assert_path_exists lib/"libsamplerate.dll.a"
    assert_path_exists bin/"libsamplerate-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/samplerate.pc").read
  end
end
