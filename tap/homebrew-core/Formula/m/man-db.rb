class ManDb < Formula
  desc "Unix documentation system"
  homepage "https://man-db.gitlab.io/man-db/"
  url "https://repo.msys2.org/msys/x86_64/man-db-2.13.1-2-x86_64.pkg.tar.zst"
  sha256 "c6c6f78004246ca4822c9070ed19a3364d6cbdc7e7e4c617309d647112e5e410"
  license "GPL-2.0-or-later"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://download.savannah.gnu.org/releases/man-db/"
    regex(/href=.*?man-db[._-]v?(\d+(?:\.\d+)+)\.t/i)
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

  resource "groff" do
    url "https://repo.msys2.org/msys/x86_64/groff-1.24.2-1-x86_64.pkg.tar.zst"
    sha256 "133760d1e5399b8f91a55cdf650d673db6d024751b995823d52712a0faf64597"
  end

  resource "less" do
    url "https://repo.msys2.org/msys/x86_64/less-710-1-x86_64.pkg.tar.zst"
    sha256 "894a643ecd01861490e5767873d67b55cd5a2faf2c77b0b24a62dbba7bd038fd"
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

  resource "libpcre2_8" do
    url "https://repo.msys2.org/msys/x86_64/libpcre2_8-10.49-1-x86_64.pkg.tar.zst"
    sha256 "2de42bf4656e3952a72809663e85899be76c7020e016687055ac0acd05145a3b"
  end

  resource "libpipeline" do
    url "https://repo.msys2.org/msys/x86_64/libpipeline-1.5.8-1-x86_64.pkg.tar.zst"
    sha256 "3a872f7f37303409f1b546964ac146dffb9b33e92e42e0219b61157e69830e28"
  end

  resource "libreadline" do
    url "https://repo.msys2.org/msys/x86_64/libreadline-8.3.006-1-x86_64.pkg.tar.zst"
    sha256 "29e25c50bf6cd7a5e6f2253af76f54fe3c1a915313c7c18e98bedaa665c6adea"
  end

  resource "libxcrypt" do
    url "https://repo.msys2.org/msys/x86_64/libxcrypt-4.5.2-1-x86_64.pkg.tar.zst"
    sha256 "e63db0aa2b708359e25f61f506e2d2c92c315febf5f88cc9f386e4ec0bdf4fa5"
  end

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  resource "perl" do
    url "https://repo.msys2.org/msys/x86_64/perl-5.42.3-3-x86_64.pkg.tar.zst"
    sha256 "6a4dbc38cdcc53d16239904b5c87d2277cffc49ed92c6a7796f8bf6b49517d5b"
  end

  resource "zlib" do
    url "https://repo.msys2.org/msys/x86_64/zlib-1.3.2-1-x86_64.pkg.tar.zst"
    sha256 "a04aa79996c57f0db936be66cf94326d7e67e9cd8dbffe4cf6e97693d0a1d9ef"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/accessdb.exe", "accessdb"
    msys2_bin "usr/bin/apropos.exe", "apropos"
    msys2_bin "usr/bin/catman.exe", "catman"
    msys2_bin "usr/bin/lexgrog.exe", "lexgrog"
    msys2_bin "usr/bin/man-recode.exe", "man-recode"
    msys2_bin "usr/bin/man.exe", "man"
    msys2_bin "usr/bin/mandb.exe", "mandb"
    msys2_bin "usr/bin/manpath.exe", "manpath"
    msys2_bin "usr/bin/whatis.exe", "whatis"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/accessdb --version 2>&1")
  end
end
