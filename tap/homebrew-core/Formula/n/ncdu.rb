class Ncdu < Formula
  desc "NCurses Disk Usage"
  homepage "https://dev.yorhel.nl/ncdu"
  version "1.23.0"
  url "https://repo.msys2.org/msys/x86_64/ncdu-1.23.0-1-x86_64.pkg.tar.zst"
  sha256 "7452a6ca97969f860081f5106fb00817b1472b83785be3bfe2147fdce8669b26"
  license "MIT"

  # ncdu 1.x (C version) via MSYS2; Homebrew ships the 2.x Zig rewrite, which has no Windows build.

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "msys2-runtime" do
    url "https://repo.msys2.org/msys/x86_64/msys2-runtime-3.6.11-3-x86_64.pkg.tar.zst"
    sha256 "b6478bd1b4cb4b2e07c45db725fee93f9424c19fa248240dcb7b7f4187cd92db"
  end

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/ncdu.exe", "ncdu"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ncdu -v")
  end
end
