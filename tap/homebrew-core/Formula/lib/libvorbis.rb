class Libvorbis < Formula
  desc "Vorbis general audio compression codec"
  homepage "https://xiph.org/vorbis/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libvorbis-1.3.7-3-any.pkg.tar.zst"
  sha256 "d013d2c7489b175299f9ea8bd9b70cf456337943b0f13e6eb470beb6c3fc251f"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://ftp.osuosl.org/pub/xiph/releases/vorbis/?C=M&O=D"
    regex(%r{href=(?:["']?|.*?/)libvorbis[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"vorbis/codec.h"
    assert_path_exists lib/"libvorbis.dll.a"
    assert_path_exists bin/"libvorbis-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/vorbis.pc").read
  end
end
