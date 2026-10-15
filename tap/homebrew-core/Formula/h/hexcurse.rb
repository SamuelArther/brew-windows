class Hexcurse < Formula
  desc "Ncurses-based console hex editor"
  homepage "https://github.com/LonnyGomes/hexcurse"
  url "https://repo.msys2.org/msys/x86_64/hexcurse-1.60.0-2-x86_64.pkg.tar.zst"
  sha256 "c78519a12a5d06faa15ffd3ee85cdfe5f1e2bd881589132c45f1ffdd467ee64e"
  license "GPL-2.0-or-later"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://github.com/LonnyGomes/hexcurse/archive/refs/tags/v1.60.0.tar.gz"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/hexcurse.exe", "hexcurse"
  end

  test do
    system bin/"hexcurse", "-help"
  end
end
