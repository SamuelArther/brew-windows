class Libdiscid < Formula
  desc "C library for creating MusicBrainz and freedb disc IDs"
  homepage "https://musicbrainz.org/doc/libdiscid"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libdiscid-0.6.5-3-any.pkg.tar.zst"
  sha256 "d22fd8bca8b4f885ce55ffa95916a7b2403c46d5bdee0251f3e8fbfff007c224"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://ftp.musicbrainz.org/pub/musicbrainz/libdiscid/"
    regex(/href=.*?libdiscid[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"discid/discid.h"
    assert_path_exists lib/"libdiscid.dll.a"
    assert_path_exists bin/"libdiscid-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libdiscid.pc").read
  end
end
