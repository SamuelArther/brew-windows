class Libxmp < Formula
  desc "C library for playback of module music (MOD, S3M, IT, etc)"
  homepage "https://xmp.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libxmp-4.7.4-1-any.pkg.tar.zst"
  sha256 "04bbcc9a048c69a86d70632d50d0cd1eb0491ff2fe7a9e1630e114610849a656"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/xmp/libxmp/4.7.4/libxmp-4.7.4.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"xmp.h"
    assert_path_exists lib/"libxmp.dll.a"
    assert_path_exists bin/"libxmp.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libxmp.pc").read
  end
end
