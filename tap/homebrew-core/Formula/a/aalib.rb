class Aalib < Formula
  desc "Portable ASCII art graphics library"
  homepage "https://aa-project.sourceforge.net/aalib/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aalib-1.4rc5-3-any.pkg.tar.zst"
  sha256 "d3b59c0f4974baa6fa85b3b5be5360fde8797ec30ce027c1d823ad414515f9c2"
  license "LGPL-2.0-or-later"
  revision 2

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://sourceforge.net/projects/aa-project/rss?path=/aa-lib"
    regex(%r{url=.*?/aalib[._-]v?(\d+(?:\.\d+)+.*?)\.t}i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/aafire.exe", "aafire"
    msys2_bin "ucrt64/bin/aainfo.exe", "aainfo"
    msys2_bin "ucrt64/bin/aasavefont.exe", "aasavefont"
    msys2_bin "ucrt64/bin/aatest.exe", "aatest"
  end

  test do
    assert_path_exists bin/"aafire.exe"
  end
end
