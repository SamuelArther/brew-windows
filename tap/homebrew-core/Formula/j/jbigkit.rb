class Jbigkit < Formula
  desc "JBIG1 data compression standard implementation"
  homepage "https://www.cl.cam.ac.uk/~mgk25/jbigkit/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-jbigkit-2.1-6-any.pkg.tar.zst"
  sha256 "9e237d1041350e7436294cfa13d4bd3d1606d1f2a32ade2eb2958b4d4e79b7ad"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url :homepage
    regex(/href=.*?jbigkit[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/jbgtopbm.exe", "jbgtopbm"
    msys2_bin "ucrt64/bin/jbgtopbm85.exe", "jbgtopbm85"
    msys2_bin "ucrt64/bin/pbmtojbg.exe", "pbmtojbg"
    msys2_bin "ucrt64/bin/pbmtojbg85.exe", "pbmtojbg85"
  end

  test do
    assert_path_exists bin/"jbgtopbm.exe"
  end
end
