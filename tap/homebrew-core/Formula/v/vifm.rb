class Vifm < Formula
  desc "Ncurses-based file manager with vi-like keybindings"
  homepage "https://vifm.info/"
  url "https://repo.msys2.org/msys/x86_64/vifm-0.14.4-1-x86_64.pkg.tar.zst"
  sha256 "e54c5ddf150662de52d25a25c144feaa1a1701ac0b94934de9f2066cbd58cefe"
  license "GPL-2.0-or-later"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://github.com/vifm/vifm/releases/download/v0.14.4/vifm-0.14.4.tar.bz2"
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
    msys2_bin "usr/bin/vifm.exe", "vifm"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/vifm --version")
  end
end
