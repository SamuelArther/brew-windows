class Libogg < Formula
  desc "Ogg Bitstream Library"
  homepage "https://www.xiph.org/ogg/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libogg-1.3.6-1-any.pkg.tar.zst"
  sha256 "110a34239e8f122da15e041b2601e2f3c6d4c909d738bdf978700601e0f64449"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://ftp.osuosl.org/pub/xiph/releases/ogg/?C=M&O=D"
    regex(%r{href=(?:["']?|.*?/)libogg[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"ogg/ogg.h"
    assert_path_exists lib/"libogg.dll.a"
    assert_path_exists bin/"libogg-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/ogg.pc").read
  end
end
