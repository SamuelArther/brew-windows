class Less < Formula
  desc "Pager program similar to more"
  homepage "https://www.greenwoodsoftware.com/less/index.html"
  url "https://repo.msys2.org/msys/x86_64/less-710-1-x86_64.pkg.tar.zst"
  version "710"
  sha256 "894a643ecd01861490e5767873d67b55cd5a2faf2c77b0b24a62dbba7bd038fd"
  license "GPL-3.0-or-later"

  # less via MSYS2.

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "libpcre2_8" do
    url "https://repo.msys2.org/msys/x86_64/libpcre2_8-10.49-1-x86_64.pkg.tar.zst"
    sha256 "2de42bf4656e3952a72809663e85899be76c7020e016687055ac0acd05145a3b"
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
    msys2_bin "usr/bin/less.exe", "less"
  end

  test do
    system bin/"less", "--version"
  end
end
