class Speexdsp < Formula
  desc "Speex audio processing library"
  homepage "https://speex.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-speexdsp-1.2.1-2-any.pkg.tar.zst"
  sha256 "397f3e3b7922cc7016c965ba42a67c22372ab8e0c4c144eddd26092a562acbf8"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://ftp.osuosl.org/pub/xiph/releases/speex/?C=M&O=D"
    regex(%r{href=(?:["']?|.*?/)speexdsp[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"speex/speex_echo.h"
    assert_path_exists lib/"libspeexdsp.dll.a"
    assert_path_exists bin/"libspeexdsp-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/speexdsp.pc").read
  end
end
