class Cmatrix < Formula
  desc "Console Matrix"
  homepage "https://github.com/abishekvashok/cmatrix/"
  url "https://repo.msys2.org/msys/x86_64/cmatrix-2.0-1-x86_64.pkg.tar.zst"
  sha256 "dfc06cba10e120a9b936f06b835efd6d12ba9df2d75bc2bee3319105940446a7"
  license "GPL-3.0-or-later"

  # cmatrix via MSYS2; needs a real console.

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
    msys2_bin "usr/bin/cmatrix.exe", "cmatrix"
  end

  test do
    system bin/"cmatrix", "-V"
  end
end
