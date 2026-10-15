class Libmediainfo < Formula
  desc "Shared library for mediainfo"
  homepage "https://mediaarea.net/en/MediaInfo"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libmediainfo-26.05-1-any.pkg.tar.zst"
  sha256 "c9654490aba85be2bbc1c57f4d5adbe45e3dedcc8e7add3cb7ef43a0e1b51c8d"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://mediaarea.net/en/MediaInfo/Download/Source"
    regex(/href=.*?libmediainfo[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"MediaInfo/MediaInfo.h"
    assert_path_exists lib/"libmediainfo.dll.a"
    assert_path_exists bin/"libmediainfo-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libmediainfo.pc").read
  end
end
