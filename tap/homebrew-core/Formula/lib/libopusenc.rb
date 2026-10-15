class Libopusenc < Formula
  desc "Convenience library for creating .opus files"
  homepage "https://www.opus-codec.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libopusenc-0.3-1-any.pkg.tar.zst"
  sha256 "c6e4de1c49e982afc7df6def7440222963b0c71291f54afea250af1c89f87b22"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://ftp.osuosl.org/pub/xiph/releases/opus/?C=M&O=D"
    regex(%r{href=(?:["']?|.*?/)libopusenc[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"opus/opusenc.h"
    assert_path_exists lib/"libopusenc.dll.a"
    assert_path_exists bin/"libopusenc-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libopusenc.pc").read
  end
end
