class Findutils < Formula
  desc "Collection of GNU find, xargs, and locate"
  homepage "https://www.gnu.org/software/findutils/"
  url "https://repo.msys2.org/msys/x86_64/findutils-4.11.0-2-x86_64.pkg.tar.zst"
  sha256 "f19d3c6b1b073d4c5759ae83f5639d11a5f0227b772f7d86d600db573d9d5d32"
  license "GPL-3.0-or-later"

  # GNU findutils via MSYS2, exposed unprefixed and with Homebrew's g prefix (Windows has its own find.exe).

  resource "bash" do
    url "https://repo.msys2.org/msys/x86_64/bash-5.3.20-2-x86_64.pkg.tar.zst"
    sha256 "0951e809c552dd7dd2835df1b2d590822187f14e717d809e942e4eb677d0adfd"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "libiconv" do
    url "https://repo.msys2.org/msys/x86_64/libiconv-1.19-1-x86_64.pkg.tar.zst"
    sha256 "0fa55ea2a6ccf97cf8c58b24b2615815e15e16e6e4e888091c263c2c83c5313d"
  end

  resource "libintl" do
    url "https://repo.msys2.org/msys/x86_64/libintl-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "336d66b9d95cf9c1804958f8e260762a3e83bf158ed5981f783bc772a31073cf"
  end

  resource "msys2-runtime" do
    url "https://repo.msys2.org/msys/x86_64/msys2-runtime-3.6.11-3-x86_64.pkg.tar.zst"
    sha256 "b6478bd1b4cb4b2e07c45db725fee93f9424c19fa248240dcb7b7f4187cd92db"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/find.exe", "find"
    msys2_bin "usr/bin/xargs.exe", "xargs"
    msys2_bin "usr/bin/locate.exe", "locate"
    msys2_bin "usr/bin/updatedb", "updatedb"
    msys2_bin "usr/bin/find.exe", "gfind"
    msys2_bin "usr/bin/xargs.exe", "gxargs"
    msys2_bin "usr/bin/locate.exe", "glocate"
    msys2_bin "usr/bin/updatedb", "gupdatedb"
  end

  test do
    touch "HOMEBREW"
    assert_match "HOMEBREW", shell_output("#{bin}/find .")
  end
end
