class Libid3tag < Formula
  desc "ID3 tag manipulation library"
  homepage "https://codeberg.org/tenacityteam/libid3tag"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libid3tag-0.16.4-1-any.pkg.tar.zst"
  sha256 "6735f3a504e59d87e88362ed42aa29584d597513052a43689b09505894dbf363"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://codeberg.org/tenacityteam/libid3tag/releases/download/0.16.4/id3tag-0.16.4-source.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"id3tag.h"
    assert_path_exists lib/"libid3tag.dll.a"
    assert_path_exists bin/"libid3tag-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/id3tag.pc").read
  end
end
