class Mutt < Formula
  desc "Mongrel of mail user agents (part elm, pine, mush, mh, etc.)"
  homepage "http://www.mutt.org/"
  url "https://repo.msys2.org/msys/x86_64/mutt-2.4.3-1-x86_64.pkg.tar.zst"
  sha256 "5610c96e04cb74c765e711ea4e9135ab4a67bdfe4f6ff3920cb0fc70577a2b72"
  license "GPL-2.0-or-later"
  revision 1

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "http://www.mutt.org/download.html"
    regex(/href=.*?mutt[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  resource "bash" do
    url "https://repo.msys2.org/msys/x86_64/bash-5.3.20-2-x86_64.pkg.tar.zst"
    sha256 "0951e809c552dd7dd2835df1b2d590822187f14e717d809e942e4eb677d0adfd"
  end

  resource "brotli" do
    url "https://repo.msys2.org/msys/x86_64/brotli-1.2.0-1-x86_64.pkg.tar.zst"
    sha256 "6642bd8fcb238119b167038614ad4eaf44a25b4978a34f74349e0359565ac671"
  end

  resource "bzip2" do
    url "https://repo.msys2.org/msys/x86_64/bzip2-1.0.8-4-x86_64.pkg.tar.zst"
    sha256 "635c483775de60663bd38859bb4624e6aaa1086354acf8e36513b75c24f64b3c"
  end

  resource "ca-certificates" do
    url "https://repo.msys2.org/msys/x86_64/ca-certificates-20260816-1-any.pkg.tar.zst"
    sha256 "b7b2344aa87022919d6c4fd2bd9410e6490835c9a2d43092c1284186bae8c5d0"
  end

  resource "coreutils" do
    url "https://repo.msys2.org/msys/x86_64/coreutils-8.32-5-x86_64.pkg.tar.zst"
    sha256 "62dfee1c39fd15f99c39802b35e82643bc14fffc16d6c76d4001caa385ec77e3"
  end

  resource "findutils" do
    url "https://repo.msys2.org/msys/x86_64/findutils-4.11.0-2-x86_64.pkg.tar.zst"
    sha256 "f19d3c6b1b073d4c5759ae83f5639d11a5f0227b772f7d86d600db573d9d5d32"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "gmp" do
    url "https://repo.msys2.org/msys/x86_64/gmp-6.3.0-2-x86_64.pkg.tar.zst"
    sha256 "f84536138e618d47a1df2da83916cfebbd449f159ffad52f994a71060a41cf34"
  end

  resource "gnupg" do
    url "https://repo.msys2.org/msys/x86_64/gnupg-2.4.9-3-x86_64.pkg.tar.zst"
    sha256 "ff78c8391e894c53ad0e3054f1e885e34f22072ad7f8578cd199e0d5657592b2"
  end

  resource "gzip" do
    url "https://repo.msys2.org/msys/x86_64/gzip-1.15-1-x86_64.pkg.tar.zst"
    sha256 "3f967c87cf041391d0d43819c91174742917ccc2d1bb9419050df29ef22f4bfd"
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

  resource "libassuan" do
    url "https://repo.msys2.org/msys/x86_64/libassuan-3.0.2-1-x86_64.pkg.tar.zst"
    sha256 "f6414e08f432e2fb130b23deb3c5c99f68c1f09768599f634c6138db50d1823f"
  end

  resource "libbz2" do
    url "https://repo.msys2.org/msys/x86_64/libbz2-1.0.8-4-x86_64.pkg.tar.zst"
    sha256 "89fb0e1478b22b80effda55ab4ae7549388d01245d3841a096d8a2d63236c2a1"
  end

  resource "libcurl" do
    url "https://repo.msys2.org/msys/x86_64/libcurl-8.23.0-1-x86_64.pkg.tar.zst"
    sha256 "3034aae7fb58e129c7af62a7e7d2ae12e0a3475a5310d7fdad38fe9a3c672c9d"
  end

  resource "libdb" do
    url "https://repo.msys2.org/msys/x86_64/libdb-6.2.32-6-x86_64.pkg.tar.zst"
    sha256 "27a35786f3aab14e625c02df7bcd8a9dda0da782ce4f0549a427a21316b4d179"
  end

  resource "libedit" do
    url "https://repo.msys2.org/msys/x86_64/libedit-20240808_3.1-1-x86_64.pkg.tar.zst"
    sha256 "aada8ba7dfe41fdcac28d3706c0dc7c621b7667035a5e629cb5f3af7fb3f7de0"
  end

  resource "libffi" do
    url "https://repo.msys2.org/msys/x86_64/libffi-3.8.0-1-x86_64.pkg.tar.zst"
    sha256 "e0a21ae5ee3f0ce1ff3f446ea2c95ad462803081014aecfa8fc1cd22bc4df187"
  end

  resource "libgcrypt" do
    url "https://repo.msys2.org/msys/x86_64/libgcrypt-1.12.4-1-x86_64.pkg.tar.zst"
    sha256 "640aafc35d064279de3529679feccecced41267923f4e6b77e3b3dbe8614aa88"
  end

  resource "libgdbm" do
    url "https://repo.msys2.org/msys/x86_64/libgdbm-1.26-1-x86_64.pkg.tar.zst"
    sha256 "bb4b08b720dd3194db010589812e9c63bec3d29b32545e7cbd07b6443e51c5ed"
  end

  resource "libgnutls" do
    url "https://repo.msys2.org/msys/x86_64/libgnutls-3.8.13-2-x86_64.pkg.tar.zst"
    sha256 "144a34bda1456dfbc0ca83ce8ed1ce49e89d750987ef096ab4083676dbcb373e"
  end

  resource "libgpg-error" do
    url "https://repo.msys2.org/msys/x86_64/libgpg-error-1.61-1-x86_64.pkg.tar.zst"
    sha256 "4c760c81be9c3875837f9b3c2a61e2c6247cbc73903e2c8696fb58df0d683e43"
  end

  resource "libgpgme" do
    url "https://repo.msys2.org/msys/x86_64/libgpgme-2.0.1-1-x86_64.pkg.tar.zst"
    sha256 "6fba98d08704aedfef3fe59d517231f314c65f5f3e42ee4eda67e15d7915948d"
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

  resource "libksba" do
    url "https://repo.msys2.org/msys/x86_64/libksba-1.8.1-1-x86_64.pkg.tar.zst"
    sha256 "fd0e37456f563504740afab92ddbf8372471f6c0fea557dc60ec08ce8bd745ad"
  end

  resource "libnettle" do
    url "https://repo.msys2.org/msys/x86_64/libnettle-4.0-1-x86_64.pkg.tar.zst"
    sha256 "aa2a398eb5af88247f29d5de683c6d1d7411733c07cc5e6c62803e1e77337cf7"
  end

  resource "libnghttp2" do
    url "https://repo.msys2.org/msys/x86_64/libnghttp2-1.70.0-1-x86_64.pkg.tar.zst"
    sha256 "9b3443ff1b26a3ece9394409efca60f3ac8eee94309e5be1e6b05706f3386110"
  end

  resource "libnghttp3" do
    url "https://repo.msys2.org/msys/x86_64/libnghttp3-1.18.0-1-x86_64.pkg.tar.zst"
    sha256 "45fdd8613fa1da08f6c0504377194497fea540ed1fec438bc557736459040669"
  end

  resource "libngtcp2" do
    url "https://repo.msys2.org/msys/x86_64/libngtcp2-1.25.0-1-x86_64.pkg.tar.zst"
    sha256 "a4c389078632e64ac2b320473651e80d44c97f9fed7a746a232d95b400b14fe8"
  end

  resource "libnpth" do
    url "https://repo.msys2.org/msys/x86_64/libnpth-1.8-1-x86_64.pkg.tar.zst"
    sha256 "29803ea796a2766396351d4c7786e33f7a64ebe2576968ec5dd55338eada08fb"
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

  resource "libpsl" do
    url "https://repo.msys2.org/msys/x86_64/libpsl-0.23.2-1-x86_64.pkg.tar.zst"
    sha256 "749014f86a1ae5f5a5adccec518e0296a03a838db9874aa9803dffaa8d30abc8"
  end

  resource "libreadline" do
    url "https://repo.msys2.org/msys/x86_64/libreadline-8.3.006-1-x86_64.pkg.tar.zst"
    sha256 "29e25c50bf6cd7a5e6f2253af76f54fe3c1a915313c7c18e98bedaa665c6adea"
  end

  resource "libsasl" do
    url "https://repo.msys2.org/msys/x86_64/libsasl-2.1.28-5-x86_64.pkg.tar.zst"
    sha256 "c93c8c7279fcfc76c7d5339bb5bb4659e36114e45fd174d81838e41a1f4c6e1e"
  end

  resource "libsqlite" do
    url "https://repo.msys2.org/msys/x86_64/libsqlite-3.54.0-1-x86_64.pkg.tar.zst"
    sha256 "089e17fe8647da22b790dc2d5629d56cf53b5da5685d6555cb172400f32fe70f"
  end

  resource "libssh2" do
    url "https://repo.msys2.org/msys/x86_64/libssh2-1.11.1-1-x86_64.pkg.tar.zst"
    sha256 "f79883af85a2bb4778e1352c0b7ea27737a05e4110ef14461f0a842a40ff893d"
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

  resource "libzstd" do
    url "https://repo.msys2.org/msys/x86_64/libzstd-1.5.7-1-x86_64.pkg.tar.zst"
    sha256 "5e370b2e725a0a7e5ff58d84653c9a0cd5a10b9cfe930f2d949a28ad2a11c1f2"
  end

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  resource "nettle" do
    url "https://repo.msys2.org/msys/x86_64/nettle-4.0-1-x86_64.pkg.tar.zst"
    sha256 "6e8809a2dc5f98dd199a3dd7c7e3783bdaf2fec1699a0c5b9eb4480ab7dd6365"
  end

  resource "openssl" do
    url "https://repo.msys2.org/msys/x86_64/openssl-3.6.5-1-x86_64.pkg.tar.zst"
    sha256 "4dc7c1199010b26d6382c59d8b16a9dbc857d314d76d9668f0f333bc5fecdd38"
  end

  resource "p11-kit" do
    url "https://repo.msys2.org/msys/x86_64/p11-kit-0.26.5-1-x86_64.pkg.tar.zst"
    sha256 "db341cc3e91c68f5de25d9a2eda3beb752ec8ac74a89f001f60453c817ce5f51"
  end

  resource "pinentry" do
    url "https://repo.msys2.org/msys/x86_64/pinentry-1.3.3-1-x86_64.pkg.tar.zst"
    sha256 "6a72ff654a57181dbb168951a24147dd6cf3f53c0df6250b96a4cca6eeb4837c"
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
    msys2_bin "usr/bin/mutt.exe", "mutt"
    msys2_bin "usr/bin/mutt_pgpring.exe", "mutt_pgpring"
    msys2_bin "usr/bin/pgpewrap.exe", "pgpewrap"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mutt --version 2>&1")
  end
end
