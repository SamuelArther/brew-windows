class Lame < Formula
  desc "High quality MPEG Audio Layer III (MP3) encoder"
  homepage "https://lame.sourceforge.io/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lame-3.100-3-any.pkg.tar.zst"
  sha256 "6bc4bf7d2f39a941a695b228818a8c846a53c43c20117e0707e08977b0dd0da9"
  license "LGPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://downloads.sourceforge.net/project/lame/lame/4.0/lame-4.0.tar.gz"
    regex(%r{url=.*?/lame[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  resource "mingw-w64-ucrt-x86_64-libiconv" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libiconv-1.19-1-any.pkg.tar.zst"
    sha256 "9a500f38c2b91808741c62fae746b3e9110b33a1ecf5c30fa0c66dbedddf7e16"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/lame.exe", "lame"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/lame --version 2>&1")
  end
end
