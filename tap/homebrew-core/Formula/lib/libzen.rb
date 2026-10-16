class Libzen < Formula
  desc "Shared library for libmediainfo"
  homepage "https://github.com/MediaArea/ZenLib"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libzen-0.4.41-3-any.pkg.tar.zst"
  sha256 "c54d6e3f4f4303f31bd965cb88bf88e14d949133b00244b3ae9a6ed4868be577"
  license "Zlib"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://mediaarea.net/en/MediaInfo/Download/Source"
    regex(/href=.*?libzen[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"ZenLib/Dir.h"
    assert_path_exists lib/"libzen.dll.a"
    assert_path_exists bin/"libzen.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libzen.pc").read
  end
end
