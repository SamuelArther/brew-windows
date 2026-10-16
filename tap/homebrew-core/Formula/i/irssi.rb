class Irssi < Formula
  desc "Modular IRC client"
  homepage "https://irssi.org/"
  url "https://repo.msys2.org/msys/x86_64/irssi-1.4.5-5-x86_64.pkg.tar.zst"
  sha256 "580cc306ef7127ca93e3b93b7b999d797f2e0d8d59dcacc77e3b7b919eb48be2"
  license "GPL-2.0-or-later" => { with: "openvpn-openssl-exception" }
  revision 3

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://github.com/irssi/irssi/releases/download/1.4.5/irssi-1.4.5.tar.xz"
    strategy :github_latest
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

  resource "gettext" do
    url "https://repo.msys2.org/msys/x86_64/gettext-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "b31ab065c3538ecb1fb6ab48ab2b3049243d6d9e585331c882f38326eefc1831"
  end

  resource "glib2" do
    url "https://repo.msys2.org/msys/x86_64/glib2-2.90.1-1-x86_64.pkg.tar.zst"
    sha256 "eb2280b5b4f921285b24c3267effecabbf3992bc37d77f2c3eac6028f35cab7a"
  end

  resource "gmp" do
    url "https://repo.msys2.org/msys/x86_64/gmp-6.3.0-2-x86_64.pkg.tar.zst"
    sha256 "f84536138e618d47a1df2da83916cfebbd449f159ffad52f994a71060a41cf34"
  end

  resource "libasprintf" do
    url "https://repo.msys2.org/msys/x86_64/libasprintf-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "fdd59ae72d355ae1c5615f12429493611782f3e38bf1f7ca9ca83b61fcfb6c26"
  end

  resource "libdb" do
    url "https://repo.msys2.org/msys/x86_64/libdb-6.2.32-6-x86_64.pkg.tar.zst"
    sha256 "27a35786f3aab14e625c02df7bcd8a9dda0da782ce4f0549a427a21316b4d179"
  end

  resource "libffi" do
    url "https://repo.msys2.org/msys/x86_64/libffi-3.8.0-1-x86_64.pkg.tar.zst"
    sha256 "e0a21ae5ee3f0ce1ff3f446ea2c95ad462803081014aecfa8fc1cd22bc4df187"
  end

  resource "libgdbm" do
    url "https://repo.msys2.org/msys/x86_64/libgdbm-1.26-1-x86_64.pkg.tar.zst"
    sha256 "bb4b08b720dd3194db010589812e9c63bec3d29b32545e7cbd07b6443e51c5ed"
  end

  resource "libgettextpo" do
    url "https://repo.msys2.org/msys/x86_64/libgettextpo-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "a9f3ef6b3712e20f0dc6eecaf8c2ec6aff5f259b2dbe87422a0aaca7b27c98d5"
  end

  resource "libiconv" do
    url "https://repo.msys2.org/msys/x86_64/libiconv-1.19-1-x86_64.pkg.tar.zst"
    sha256 "0fa55ea2a6ccf97cf8c58b24b2615815e15e16e6e4e888091c263c2c83c5313d"
  end

  resource "libintl" do
    url "https://repo.msys2.org/msys/x86_64/libintl-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "336d66b9d95cf9c1804958f8e260762a3e83bf158ed5981f783bc772a31073cf"
  end

  resource "libopenssl" do
    url "https://repo.msys2.org/msys/x86_64/libopenssl-3.6.5-1-x86_64.pkg.tar.zst"
    sha256 "05f6cc946f0dde3aecc3632007fa1d874e40720226117b750327bc2a01846325"
  end

  resource "libpcre2_8" do
    url "https://repo.msys2.org/msys/x86_64/libpcre2_8-10.49-1-x86_64.pkg.tar.zst"
    sha256 "2de42bf4656e3952a72809663e85899be76c7020e016687055ac0acd05145a3b"
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

  resource "openssl" do
    url "https://repo.msys2.org/msys/x86_64/openssl-3.6.5-1-x86_64.pkg.tar.zst"
    sha256 "4dc7c1199010b26d6382c59d8b16a9dbc857d314d76d9668f0f333bc5fecdd38"
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
    msys2_bin "usr/bin/irssi.exe", "irssi"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/irssi --version 2>&1")
  end
end
