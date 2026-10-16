class Lftp < Formula
  desc "Sophisticated file transfer program"
  homepage "https://lftp.yar.ru/"
  url "https://repo.msys2.org/msys/x86_64/lftp-4.9.3-1-x86_64.pkg.tar.zst"
  sha256 "1f831bbfdd2b29d6cd54bea849f3f3f5f8ce5fbe45fbcce9a670c63d930e92cf"
  license "GPL-3.0-or-later"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://github.com/lavv17/lftp/releases/download/v4.9.3/lftp-4.9.3.tar.gz"
  end

  resource "bash" do
    url "https://repo.msys2.org/msys/x86_64/bash-5.3.20-2-x86_64.pkg.tar.zst"
    sha256 "0951e809c552dd7dd2835df1b2d590822187f14e717d809e942e4eb677d0adfd"
  end

  resource "ca-certificates" do
    url "https://repo.msys2.org/msys/x86_64/ca-certificates-20260816-1-any.pkg.tar.zst"
    sha256 "b7b2344aa87022919d6c4fd2bd9410e6490835c9a2d43092c1284186bae8c5d0"
  end

  resource "coreutils" do
    url "https://repo.msys2.org/msys/x86_64/coreutils-8.32-5-x86_64.pkg.tar.zst"
    sha256 "62dfee1c39fd15f99c39802b35e82643bc14fffc16d6c76d4001caa385ec77e3"
  end

  resource "expat" do
    url "https://repo.msys2.org/msys/x86_64/expat-2.9.0-1-x86_64.pkg.tar.zst"
    sha256 "20374d2f31907c90a238d19b108e3442a06913a5f20214492a673bdd89ac15d1"
  end

  resource "findutils" do
    url "https://repo.msys2.org/msys/x86_64/findutils-4.11.0-2-x86_64.pkg.tar.zst"
    sha256 "f19d3c6b1b073d4c5759ae83f5639d11a5f0227b772f7d86d600db573d9d5d32"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "gettext" do
    url "https://repo.msys2.org/msys/x86_64/gettext-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "b31ab065c3538ecb1fb6ab48ab2b3049243d6d9e585331c882f38326eefc1831"
  end

  resource "gmp" do
    url "https://repo.msys2.org/msys/x86_64/gmp-6.3.0-2-x86_64.pkg.tar.zst"
    sha256 "f84536138e618d47a1df2da83916cfebbd449f159ffad52f994a71060a41cf34"
  end

  resource "gzip" do
    url "https://repo.msys2.org/msys/x86_64/gzip-1.15-1-x86_64.pkg.tar.zst"
    sha256 "3f967c87cf041391d0d43819c91174742917ccc2d1bb9419050df29ef22f4bfd"
  end

  resource "heimdal" do
    url "https://repo.msys2.org/msys/x86_64/heimdal-7.8.0-7-x86_64.pkg.tar.zst"
    sha256 "c1e9dc49b6eaffd145abd73c8b516acca89eac64fa90031e116ac93c3b493a1e"
  end

  resource "heimdal-libs" do
    url "https://repo.msys2.org/msys/x86_64/heimdal-libs-7.8.0-7-x86_64.pkg.tar.zst"
    sha256 "badbf9d464fe8498668451b8d53cf84c7ec3bd0e7bcc0ea158bf6339a30a2416"
  end

  resource "info" do
    url "https://repo.msys2.org/msys/x86_64/info-7.2-3-x86_64.pkg.tar.zst"
    sha256 "69242603357aa7011356f7d15a2017ad086e5682c8577f13520943c94bd2b03e"
  end

  resource "less" do
    url "https://repo.msys2.org/msys/x86_64/less-710-1-x86_64.pkg.tar.zst"
    sha256 "894a643ecd01861490e5767873d67b55cd5a2faf2c77b0b24a62dbba7bd038fd"
  end

  resource "libasprintf" do
    url "https://repo.msys2.org/msys/x86_64/libasprintf-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "fdd59ae72d355ae1c5615f12429493611782f3e38bf1f7ca9ca83b61fcfb6c26"
  end

  resource "libcbor" do
    url "https://repo.msys2.org/msys/x86_64/libcbor-0.14.0-2-x86_64.pkg.tar.zst"
    sha256 "a661826edc2d452f8d74bae2cd5974dfbcd84e23bfbfa7ea3db38ee010d7b004"
  end

  resource "libdb" do
    url "https://repo.msys2.org/msys/x86_64/libdb-6.2.32-6-x86_64.pkg.tar.zst"
    sha256 "27a35786f3aab14e625c02df7bcd8a9dda0da782ce4f0549a427a21316b4d179"
  end

  resource "libedit" do
    url "https://repo.msys2.org/msys/x86_64/libedit-20240808_3.1-1-x86_64.pkg.tar.zst"
    sha256 "aada8ba7dfe41fdcac28d3706c0dc7c621b7667035a5e629cb5f3af7fb3f7de0"
  end

  resource "libexpat" do
    url "https://repo.msys2.org/msys/x86_64/libexpat-2.9.0-1-x86_64.pkg.tar.zst"
    sha256 "3021e1119cbe6a384977914c1dc099a98ce861505cc07229a842e8e1253597e5"
  end

  resource "libffi" do
    url "https://repo.msys2.org/msys/x86_64/libffi-3.8.0-1-x86_64.pkg.tar.zst"
    sha256 "e0a21ae5ee3f0ce1ff3f446ea2c95ad462803081014aecfa8fc1cd22bc4df187"
  end

  resource "libfido2" do
    url "https://repo.msys2.org/msys/x86_64/libfido2-1.17.0-1-x86_64.pkg.tar.zst"
    sha256 "30794db36fa21af67c7126a221e2cb95b177be08845bb38720a8c9f28c39cdbb"
  end

  resource "libgettextpo" do
    url "https://repo.msys2.org/msys/x86_64/libgettextpo-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "a9f3ef6b3712e20f0dc6eecaf8c2ec6aff5f259b2dbe87422a0aaca7b27c98d5"
  end

  resource "libgnutls" do
    url "https://repo.msys2.org/msys/x86_64/libgnutls-3.8.13-2-x86_64.pkg.tar.zst"
    sha256 "144a34bda1456dfbc0ca83ce8ed1ce49e89d750987ef096ab4083676dbcb373e"
  end

  resource "libhogweed" do
    url "https://repo.msys2.org/msys/x86_64/libhogweed-4.0-1-x86_64.pkg.tar.zst"
    sha256 "1302b7e86c5c54c93bd431685ccefa89df5f1b01257c68e24f2609df953cfdc2"
  end

  resource "libiconv" do
    url "https://repo.msys2.org/msys/x86_64/libiconv-1.19-1-x86_64.pkg.tar.zst"
    sha256 "0fa55ea2a6ccf97cf8c58b24b2615815e15e16e6e4e888091c263c2c83c5313d"
  end

  resource "libidn2" do
    url "https://repo.msys2.org/msys/x86_64/libidn2-2.3.8-1-x86_64.pkg.tar.zst"
    sha256 "55e0de96b893e9f67f14a4283d411fc2a45d7b59333bc16e2a9b16e5bfc9282f"
  end

  resource "libintl" do
    url "https://repo.msys2.org/msys/x86_64/libintl-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "336d66b9d95cf9c1804958f8e260762a3e83bf158ed5981f783bc772a31073cf"
  end

  resource "libnettle" do
    url "https://repo.msys2.org/msys/x86_64/libnettle-4.0-1-x86_64.pkg.tar.zst"
    sha256 "aa2a398eb5af88247f29d5de683c6d1d7411733c07cc5e6c62803e1e77337cf7"
  end

  resource "libopenssl" do
    url "https://repo.msys2.org/msys/x86_64/libopenssl-3.6.5-1-x86_64.pkg.tar.zst"
    sha256 "05f6cc946f0dde3aecc3632007fa1d874e40720226117b750327bc2a01846325"
  end

  resource "libp11-kit" do
    url "https://repo.msys2.org/msys/x86_64/libp11-kit-0.26.5-1-x86_64.pkg.tar.zst"
    sha256 "62707348cea67bf4ee009dbf25548c84e9f06a76b688590bd14e76321d58e125"
  end

  resource "libpcre2_8" do
    url "https://repo.msys2.org/msys/x86_64/libpcre2_8-10.49-1-x86_64.pkg.tar.zst"
    sha256 "2de42bf4656e3952a72809663e85899be76c7020e016687055ac0acd05145a3b"
  end

  resource "libreadline" do
    url "https://repo.msys2.org/msys/x86_64/libreadline-8.3.006-1-x86_64.pkg.tar.zst"
    sha256 "29e25c50bf6cd7a5e6f2253af76f54fe3c1a915313c7c18e98bedaa665c6adea"
  end

  resource "libsqlite" do
    url "https://repo.msys2.org/msys/x86_64/libsqlite-3.54.0-1-x86_64.pkg.tar.zst"
    sha256 "089e17fe8647da22b790dc2d5629d56cf53b5da5685d6555cb172400f32fe70f"
  end

  resource "libtasn1" do
    url "https://repo.msys2.org/msys/x86_64/libtasn1-4.21.0-1-x86_64.pkg.tar.zst"
    sha256 "14f15c4b2b6ebeb6bfcf060cab27a32cd8967f70b0e68cc14670062cce546f04"
  end

  resource "libunistring" do
    url "https://repo.msys2.org/msys/x86_64/libunistring-1.4.2-1-x86_64.pkg.tar.zst"
    sha256 "bcd04706d95b5e7127d39834565bceed6b400192f5fb51599745aba28d5da018"
  end

  resource "libxcrypt" do
    url "https://repo.msys2.org/msys/x86_64/libxcrypt-4.5.2-1-x86_64.pkg.tar.zst"
    sha256 "e63db0aa2b708359e25f61f506e2d2c92c315febf5f88cc9f386e4ec0bdf4fa5"
  end

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  resource "openssh" do
    url "https://repo.msys2.org/msys/x86_64/openssh-10.6p1-1-x86_64.pkg.tar.zst"
    sha256 "40694d9506e733f55262c4123cf6107dd28d27457a2b320f2866cdbde454c448"
  end

  resource "openssl" do
    url "https://repo.msys2.org/msys/x86_64/openssl-3.6.5-1-x86_64.pkg.tar.zst"
    sha256 "4dc7c1199010b26d6382c59d8b16a9dbc857d314d76d9668f0f333bc5fecdd38"
  end

  resource "p11-kit" do
    url "https://repo.msys2.org/msys/x86_64/p11-kit-0.26.5-1-x86_64.pkg.tar.zst"
    sha256 "db341cc3e91c68f5de25d9a2eda3beb752ec8ac74a89f001f60453c817ce5f51"
  end

  resource "sed" do
    url "https://repo.msys2.org/msys/x86_64/sed-4.9-1-x86_64.pkg.tar.zst"
    sha256 "3748af28f69e946ec5a42e6670c9bbf6da7352dc93baaa537f69e99c5483b9fc"
  end

  resource "zlib" do
    url "https://repo.msys2.org/msys/x86_64/zlib-1.3.2-1-x86_64.pkg.tar.zst"
    sha256 "a04aa79996c57f0db936be66cf94326d7e67e9cd8dbffe4cf6e97693d0a1d9ef"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/lftp.exe", "lftp"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/lftp --version 2>&1")
  end
end
