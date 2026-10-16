class Autogen < Formula
  desc "Automated text file generator"
  homepage "https://autogen.sourceforge.net/"
  url "https://repo.msys2.org/msys/x86_64/autogen-5.18.16-6-x86_64.pkg.tar.zst"
  sha256 "7c5338c38ef1aa1a86b0fb6d173fc32b7c127eba83e791d454e03456960ae968"
  license "GPL-3.0-or-later"
  revision 3

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://ftpmirror.gnu.org/autogen/rel5.18.16/autogen-5.18.16.tar.xz"
    regex(%r{href=.*?rel(\d+(?:\.\d+)+)/?["' >]}i)
  end

  resource "coreutils" do
    url "https://repo.msys2.org/msys/x86_64/coreutils-8.32-5-x86_64.pkg.tar.zst"
    sha256 "62dfee1c39fd15f99c39802b35e82643bc14fffc16d6c76d4001caa385ec77e3"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "gmp" do
    url "https://repo.msys2.org/msys/x86_64/gmp-6.3.0-2-x86_64.pkg.tar.zst"
    sha256 "f84536138e618d47a1df2da83916cfebbd449f159ffad52f994a71060a41cf34"
  end

  resource "libffi" do
    url "https://repo.msys2.org/msys/x86_64/libffi-3.8.0-1-x86_64.pkg.tar.zst"
    sha256 "e0a21ae5ee3f0ce1ff3f446ea2c95ad462803081014aecfa8fc1cd22bc4df187"
  end

  resource "libgc" do
    url "https://repo.msys2.org/msys/x86_64/libgc-8.2.12-1-x86_64.pkg.tar.zst"
    sha256 "a6bd62a8473c57d3233ab5d4baaf090259fc3a44032d7f1938600c08d2cc2f88"
  end

  resource "libguile" do
    url "https://repo.msys2.org/msys/x86_64/libguile-3.0.11-1-x86_64.pkg.tar.zst"
    sha256 "455c95505ac126fe1d15edb3983c2ffcecbaa0f5b1775dca1d022190b776edc2"
  end

  resource "libiconv" do
    url "https://repo.msys2.org/msys/x86_64/libiconv-1.19-1-x86_64.pkg.tar.zst"
    sha256 "0fa55ea2a6ccf97cf8c58b24b2615815e15e16e6e4e888091c263c2c83c5313d"
  end

  resource "libintl" do
    url "https://repo.msys2.org/msys/x86_64/libintl-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "336d66b9d95cf9c1804958f8e260762a3e83bf158ed5981f783bc772a31073cf"
  end

  resource "libreadline" do
    url "https://repo.msys2.org/msys/x86_64/libreadline-8.3.006-1-x86_64.pkg.tar.zst"
    sha256 "29e25c50bf6cd7a5e6f2253af76f54fe3c1a915313c7c18e98bedaa665c6adea"
  end

  resource "libunistring" do
    url "https://repo.msys2.org/msys/x86_64/libunistring-1.4.2-1-x86_64.pkg.tar.zst"
    sha256 "bcd04706d95b5e7127d39834565bceed6b400192f5fb51599745aba28d5da018"
  end

  resource "libxcrypt" do
    url "https://repo.msys2.org/msys/x86_64/libxcrypt-4.5.2-1-x86_64.pkg.tar.zst"
    sha256 "e63db0aa2b708359e25f61f506e2d2c92c315febf5f88cc9f386e4ec0bdf4fa5"
  end

  resource "libxml2" do
    url "https://repo.msys2.org/msys/x86_64/libxml2-2.15.4-1-x86_64.pkg.tar.zst"
    sha256 "e627c55fa4b1c0890fe7c1728b4c998979221a11e05dc1d22ca1c9266af23873"
  end

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  resource "zlib" do
    url "https://repo.msys2.org/msys/x86_64/zlib-1.3.2-1-x86_64.pkg.tar.zst"
    sha256 "a04aa79996c57f0db936be66cf94326d7e67e9cd8dbffe4cf6e97693d0a1d9ef"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/autogen.exe", "autogen"
    msys2_bin "usr/bin/columns.exe", "columns"
    msys2_bin "usr/bin/getdefs.exe", "getdefs"
    msys2_bin "usr/bin/xml2ag.exe", "xml2ag"
  end

  test do
    assert_path_exists bin/"autogen.exe"
  end
end
