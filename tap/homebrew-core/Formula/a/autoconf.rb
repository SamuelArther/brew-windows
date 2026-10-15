class Autoconf < Formula
  desc "Automatic configure script builder"
  homepage "https://gitweb.gentoo.org/proj/autotools-wrappers.git/"
  version "2.73"
  url "https://repo.msys2.org/msys/x86_64/autoconf-wrapper-20260320-2-any.pkg.tar.zst"
  sha256 "40781b015533fe25492ec64706532ec84b803f1c86fc6e4fda832da49582cc59"
  license "GPL-2.0-only"

  resource "autoconf2.69" do
    url "https://repo.msys2.org/msys/x86_64/autoconf2.69-2.69-4-any.pkg.tar.zst"
    sha256 "56b2de8e53e46a88bb217a427c2f7b5671ac6400148681fb2089e9b7be817364"
  end

  resource "autoconf2.71" do
    url "https://repo.msys2.org/msys/x86_64/autoconf2.71-2.71-4-any.pkg.tar.zst"
    sha256 "55be2b911beaa0424d60fdcf206f2411a0eb24cb53e66a82964497ef8a4faac0"
  end

  resource "autoconf2.72" do
    url "https://repo.msys2.org/msys/x86_64/autoconf2.72-2.72-4-any.pkg.tar.zst"
    sha256 "65b3a37edf8bb4221d38db910ce1acc1ed879c7bcba33faa058066d06d35e7b0"
  end

  resource "autoconf2.73" do
    url "https://repo.msys2.org/msys/x86_64/autoconf2.73-2.73-1-any.pkg.tar.zst"
    sha256 "716d0542dc12a481f1c2d5574f17f1470eb9d3226cf6e206d4e243c12fa95e1a"
  end

  resource "bash" do
    url "https://repo.msys2.org/msys/x86_64/bash-5.3.20-2-x86_64.pkg.tar.zst"
    sha256 "0951e809c552dd7dd2835df1b2d590822187f14e717d809e942e4eb677d0adfd"
  end

  resource "coreutils" do
    url "https://repo.msys2.org/msys/x86_64/coreutils-8.32-5-x86_64.pkg.tar.zst"
    sha256 "62dfee1c39fd15f99c39802b35e82643bc14fffc16d6c76d4001caa385ec77e3"
  end

  resource "db" do
    url "https://repo.msys2.org/msys/x86_64/db-6.2.32-6-x86_64.pkg.tar.zst"
    sha256 "41bc94acad3b53eaa92227ec34412305f648848ffbc963bcad88274517e13e8d"
  end

  resource "diffutils" do
    url "https://repo.msys2.org/msys/x86_64/diffutils-3.12-1-x86_64.pkg.tar.zst"
    sha256 "7902c8ce3d4dd69a0f5e98dc9d5c83c17b23314ba486169db57ef6e2835ce3b6"
  end

  resource "findutils" do
    url "https://repo.msys2.org/msys/x86_64/findutils-4.11.0-2-x86_64.pkg.tar.zst"
    sha256 "f19d3c6b1b073d4c5759ae83f5639d11a5f0227b772f7d86d600db573d9d5d32"
  end

  resource "gawk" do
    url "https://repo.msys2.org/msys/x86_64/gawk-5.4.1-1-x86_64.pkg.tar.zst"
    sha256 "4b1322cb6c83d61fde5100e229e3cabc5b7d01fc0d0d8bc311edc08df91916be"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "gdbm" do
    url "https://repo.msys2.org/msys/x86_64/gdbm-1.26-1-x86_64.pkg.tar.zst"
    sha256 "1f8e949be2bbe488c5644132db78ecda288c9945922e7ed2447384734e2adba2"
  end

  resource "gmp" do
    url "https://repo.msys2.org/msys/x86_64/gmp-6.3.0-2-x86_64.pkg.tar.zst"
    sha256 "f84536138e618d47a1df2da83916cfebbd449f159ffad52f994a71060a41cf34"
  end

  resource "grep" do
    url "https://repo.msys2.org/msys/x86_64/grep-1~3.0-7-x86_64.pkg.tar.zst"
    sha256 "2909b3bbef8d33d66980339fb7bd2d3d9145bae0f04191e6671df39868aea427"
  end

  resource "libdb" do
    url "https://repo.msys2.org/msys/x86_64/libdb-6.2.32-6-x86_64.pkg.tar.zst"
    sha256 "27a35786f3aab14e625c02df7bcd8a9dda0da782ce4f0549a427a21316b4d179"
  end

  resource "libgdbm" do
    url "https://repo.msys2.org/msys/x86_64/libgdbm-1.26-1-x86_64.pkg.tar.zst"
    sha256 "bb4b08b720dd3194db010589812e9c63bec3d29b32545e7cbd07b6443e51c5ed"
  end

  resource "libiconv" do
    url "https://repo.msys2.org/msys/x86_64/libiconv-1.19-1-x86_64.pkg.tar.zst"
    sha256 "0fa55ea2a6ccf97cf8c58b24b2615815e15e16e6e4e888091c263c2c83c5313d"
  end

  resource "libintl" do
    url "https://repo.msys2.org/msys/x86_64/libintl-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "336d66b9d95cf9c1804958f8e260762a3e83bf158ed5981f783bc772a31073cf"
  end

  resource "libpcre" do
    url "https://repo.msys2.org/msys/x86_64/libpcre-8.45-5-x86_64.pkg.tar.zst"
    sha256 "b4dc2eb7795cbacaa683101d720fa67f536262497a1d89934a58f5dab8d17d17"
  end

  resource "libreadline" do
    url "https://repo.msys2.org/msys/x86_64/libreadline-8.3.006-1-x86_64.pkg.tar.zst"
    sha256 "29e25c50bf6cd7a5e6f2253af76f54fe3c1a915313c7c18e98bedaa665c6adea"
  end

  resource "libxcrypt" do
    url "https://repo.msys2.org/msys/x86_64/libxcrypt-4.5.2-1-x86_64.pkg.tar.zst"
    sha256 "e63db0aa2b708359e25f61f506e2d2c92c315febf5f88cc9f386e4ec0bdf4fa5"
  end

  resource "m4" do
    url "https://repo.msys2.org/msys/x86_64/m4-1.4.21-1-x86_64.pkg.tar.zst"
    sha256 "b561e8871c7599502d9fae7196ee965b3fed3d7af09154eb8d2f5e4d9e5e5106"
  end

  resource "mpfr" do
    url "https://repo.msys2.org/msys/x86_64/mpfr-4.2.2-1-x86_64.pkg.tar.zst"
    sha256 "8f1b6c17c53e00494a488e03dfdf8fc742027ed03a5bd7b1b3a6a33f595a7498"
  end

  resource "msys2-runtime" do
    url "https://repo.msys2.org/msys/x86_64/msys2-runtime-3.6.11-3-x86_64.pkg.tar.zst"
    sha256 "b6478bd1b4cb4b2e07c45db725fee93f9424c19fa248240dcb7b7f4187cd92db"
  end

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  resource "perl" do
    url "https://repo.msys2.org/msys/x86_64/perl-5.42.3-3-x86_64.pkg.tar.zst"
    sha256 "6a4dbc38cdcc53d16239904b5c87d2277cffc49ed92c6a7796f8bf6b49517d5b"
  end

  resource "sed" do
    url "https://repo.msys2.org/msys/x86_64/sed-4.9-1-x86_64.pkg.tar.zst"
    sha256 "3748af28f69e946ec5a42e6670c9bbf6da7352dc93baaa537f69e99c5483b9fc"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/autoconf", "autoconf"
    msys2_bin "usr/bin/autoheader", "autoheader"
    msys2_bin "usr/bin/autom4te", "autom4te"
    msys2_bin "usr/bin/autoreconf", "autoreconf"
    msys2_bin "usr/bin/autoscan", "autoscan"
    msys2_bin "usr/bin/autoupdate", "autoupdate"
    msys2_bin "usr/bin/ifnames", "ifnames"
  end
end
