class Tig < Formula
  desc "Text interface for Git repositories"
  homepage "https://jonas.github.io/tig/"
  url "https://repo.msys2.org/msys/x86_64/tig-2.6.1-1-x86_64.pkg.tar.zst"
  sha256 "c7f514ca24a3c37a0426753a20e9a27ff6f53190df4e834b9c63e631efcfa390"
  license "GPL-2.0-or-later"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://github.com/jonas/tig/releases/download/tig-2.6.1/tig-2.6.1.tar.gz"
  end

  resource "bash" do
    url "https://repo.msys2.org/msys/x86_64/bash-5.3.20-2-x86_64.pkg.tar.zst"
    sha256 "0951e809c552dd7dd2835df1b2d590822187f14e717d809e942e4eb677d0adfd"
  end

  resource "brotli" do
    url "https://repo.msys2.org/msys/x86_64/brotli-1.2.0-1-x86_64.pkg.tar.zst"
    sha256 "6642bd8fcb238119b167038614ad4eaf44a25b4978a34f74349e0359565ac671"
  end

  resource "ca-certificates" do
    url "https://repo.msys2.org/msys/x86_64/ca-certificates-20260816-1-any.pkg.tar.zst"
    sha256 "b7b2344aa87022919d6c4fd2bd9410e6490835c9a2d43092c1284186bae8c5d0"
  end

  resource "coreutils" do
    url "https://repo.msys2.org/msys/x86_64/coreutils-8.32-5-x86_64.pkg.tar.zst"
    sha256 "62dfee1c39fd15f99c39802b35e82643bc14fffc16d6c76d4001caa385ec77e3"
  end

  resource "curl" do
    url "https://repo.msys2.org/msys/x86_64/curl-8.23.0-1-x86_64.pkg.tar.zst"
    sha256 "6f21e0187e3f309199e01f729d415ccfeb304cc9f8a27365c8aa9c216351022e"
  end

  resource "db" do
    url "https://repo.msys2.org/msys/x86_64/db-6.2.32-6-x86_64.pkg.tar.zst"
    sha256 "41bc94acad3b53eaa92227ec34412305f648848ffbc963bcad88274517e13e8d"
  end

  resource "file" do
    url "https://repo.msys2.org/msys/x86_64/file-5.48-1-x86_64.pkg.tar.zst"
    sha256 "f55026638fe4ed4b31ee3cf316843ac190d9332816bff8dcb0a99fd8c6350608"
  end

  resource "findutils" do
    url "https://repo.msys2.org/msys/x86_64/findutils-4.11.0-2-x86_64.pkg.tar.zst"
    sha256 "f19d3c6b1b073d4c5759ae83f5639d11a5f0227b772f7d86d600db573d9d5d32"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "gdbm" do
    url "https://repo.msys2.org/msys/x86_64/gdbm-1.26-1-x86_64.pkg.tar.zst"
    sha256 "1f8e949be2bbe488c5644132db78ecda288c9945922e7ed2447384734e2adba2"
  end

  resource "git" do
    url "https://repo.msys2.org/msys/x86_64/git-2.56.0-1-x86_64.pkg.tar.zst"
    sha256 "051fc526cba4a5c732acde867cddec9dbd54653d781be3009546c611bfd82638"
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

  resource "libbz2" do
    url "https://repo.msys2.org/msys/x86_64/libbz2-1.0.8-4-x86_64.pkg.tar.zst"
    sha256 "89fb0e1478b22b80effda55ab4ae7549388d01245d3841a096d8a2d63236c2a1"
  end

  resource "libcbor" do
    url "https://repo.msys2.org/msys/x86_64/libcbor-0.14.0-2-x86_64.pkg.tar.zst"
    sha256 "a661826edc2d452f8d74bae2cd5974dfbcd84e23bfbfa7ea3db38ee010d7b004"
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

  resource "libgdbm" do
    url "https://repo.msys2.org/msys/x86_64/libgdbm-1.26-1-x86_64.pkg.tar.zst"
    sha256 "bb4b08b720dd3194db010589812e9c63bec3d29b32545e7cbd07b6443e51c5ed"
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

  resource "liblzma" do
    url "https://repo.msys2.org/msys/x86_64/liblzma-5.8.4-1-x86_64.pkg.tar.zst"
    sha256 "4b7d61656e72a5651a797680b66186338dce4f8fbc62a637ab3b59922dd01074"
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

  resource "nano" do
    url "https://repo.msys2.org/msys/x86_64/nano-9.2-1-x86_64.pkg.tar.zst"
    sha256 "0fd23077af18ab656e4a082ce0455ab16217c852f3968343e903843c204a12b2"
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

  resource "perl" do
    url "https://repo.msys2.org/msys/x86_64/perl-5.42.3-3-x86_64.pkg.tar.zst"
    sha256 "6a4dbc38cdcc53d16239904b5c87d2277cffc49ed92c6a7796f8bf6b49517d5b"
  end

  resource "perl-authen-sasl" do
    url "https://repo.msys2.org/msys/x86_64/perl-authen-sasl-2.2100-2-any.pkg.tar.zst"
    sha256 "98cdd396f65df318ca777de7f4166485ad7815c5000399e4aa6843f59e00e55b"
  end

  resource "perl-clone" do
    url "https://repo.msys2.org/msys/x86_64/perl-clone-0.51-1-x86_64.pkg.tar.zst"
    sha256 "39ee27b8a2ff4062e0aa1d3cdf334e59a1a726916daaa0c4bc13d31d1788a49a"
  end

  resource "perl-convert-binhex" do
    url "https://repo.msys2.org/msys/x86_64/perl-convert-binhex-1.125-3-any.pkg.tar.zst"
    sha256 "96d9418550129e569a69b17682654f71754c92bbe97515f2f255d6aa9ad4d1bc"
  end

  resource "perl-encode-locale" do
    url "https://repo.msys2.org/msys/x86_64/perl-encode-locale-1.05-3-any.pkg.tar.zst"
    sha256 "e02957a3468521d6a35fec39327ee9ecf024b00261fb4c496339cb5428decd8f"
  end

  resource "perl-error" do
    url "https://repo.msys2.org/msys/x86_64/perl-error-0.17030-3-any.pkg.tar.zst"
    sha256 "c5c183196a4803c0b427fc174e446cc530e80833fce7405719bc46a33d3c61fd"
  end

  resource "perl-file-listing" do
    url "https://repo.msys2.org/msys/x86_64/perl-file-listing-6.16-2-any.pkg.tar.zst"
    sha256 "15e00cd7923ec76c8127576bb64e0cb159a48ea87efefc35f64493bcd1e8a95e"
  end

  resource "perl-html-parser" do
    url "https://repo.msys2.org/msys/x86_64/perl-html-parser-3.86-2-x86_64.pkg.tar.zst"
    sha256 "18a6097593b08689f3923bfe948356c645e486929af5fee6e9d661a1285c060c"
  end

  resource "perl-html-tagset" do
    url "https://repo.msys2.org/msys/x86_64/perl-html-tagset-3.24-2-any.pkg.tar.zst"
    sha256 "cf9adccdaaa6b5b1307365afed083daa126d4230ef812f35ec65eb5f6e472c92"
  end

  resource "perl-http-cookiejar" do
    url "https://repo.msys2.org/msys/x86_64/perl-http-cookiejar-0.014-2-any.pkg.tar.zst"
    sha256 "70fdf1d4bdbab078765e7200436f614cf995007f2329f487baafc1841e8d66a9"
  end

  resource "perl-http-cookies" do
    url "https://repo.msys2.org/msys/x86_64/perl-http-cookies-6.12-2-any.pkg.tar.zst"
    sha256 "902e3d2ac5564536bafd113f42d49df7ecf8c4808c793369ba7bb413a12546d1"
  end

  resource "perl-http-daemon" do
    url "https://repo.msys2.org/msys/x86_64/perl-http-daemon-6.17-2-any.pkg.tar.zst"
    sha256 "77d6fd31b6e1a1db66237a97da8a99c7a0eff8297ca1d5b75acfcb38a86251ca"
  end

  resource "perl-http-date" do
    url "https://repo.msys2.org/msys/x86_64/perl-http-date-6.08-2-any.pkg.tar.zst"
    sha256 "c26c4e9655557e7c9ec8b74c39d32e31ec5640552f5371b221d59bc22bfa520a"
  end

  resource "perl-http-message" do
    url "https://repo.msys2.org/msys/x86_64/perl-http-message-7.04-2-any.pkg.tar.zst"
    sha256 "526c89268f0489c37d950fbd8fdbdbb745dbe61ec2d2bd2871c6c8646d01792d"
  end

  resource "perl-http-negotiate" do
    url "https://repo.msys2.org/msys/x86_64/perl-http-negotiate-6.01-4-any.pkg.tar.zst"
    sha256 "0cb00eb507d51b5fb9446827acc7d7f0629381f7d7bf8548fa3e0ba64704dd5e"
  end

  resource "perl-io-html" do
    url "https://repo.msys2.org/msys/x86_64/perl-io-html-1.004-3-any.pkg.tar.zst"
    sha256 "3d42f5fe05b06fda5cbe94c79f98c9a1f32d0b0361b93839f9244af3676620f0"
  end

  resource "perl-io-socket-ssl" do
    url "https://repo.msys2.org/msys/x86_64/perl-io-socket-ssl-2.099-2-any.pkg.tar.zst"
    sha256 "d9d0adc2c9435a50d573869031f893babbceef8ddbc8039efe01a7ee598ac35f"
  end

  resource "perl-io-stringy" do
    url "https://repo.msys2.org/msys/x86_64/perl-io-stringy-2.113-4-any.pkg.tar.zst"
    sha256 "35664d5b4b6e6c569984f4901d7d57b56869b5a41b9ab35674f938186a50b7e2"
  end

  resource "perl-libwww" do
    url "https://repo.msys2.org/msys/x86_64/perl-libwww-6.83-3-any.pkg.tar.zst"
    sha256 "adbc8538af75c6d9d37ec3c05cbda27843ad35c6b9fc6de8de64e016bb4c27b1"
  end

  resource "perl-lwp-mediatypes" do
    url "https://repo.msys2.org/msys/x86_64/perl-lwp-mediatypes-6.05-2-any.pkg.tar.zst"
    sha256 "4afde66f5a53366fbed27779345b3f7fd782feffa00613a5d5482b5d207f6358"
  end

  resource "perl-mailtools" do
    url "https://repo.msys2.org/msys/x86_64/perl-mailtools-2.22-2-any.pkg.tar.zst"
    sha256 "53cba8b2bc2042518d83a88d562a9ee0d1ceae3d581ac4896baec0ac25bfc91c"
  end

  resource "perl-mime-tools" do
    url "https://repo.msys2.org/msys/x86_64/perl-mime-tools-5.518-2-any.pkg.tar.zst"
    sha256 "05d3cb6d53c943ce4481f67a04e7b9272e383577ea84672f2ff390d7dab95766"
  end

  resource "perl-net-http" do
    url "https://repo.msys2.org/msys/x86_64/perl-net-http-6.24-2-any.pkg.tar.zst"
    sha256 "8f45b20264525e3c5788f2b12a1c15ae8a27ef2904030bd598aa7fc9d10fda36"
  end

  resource "perl-net-smtp-ssl" do
    url "https://repo.msys2.org/msys/x86_64/perl-net-smtp-ssl-1.04-3-any.pkg.tar.zst"
    sha256 "d553221db395c227c93643800af42d39049b055f2630e39b1c4cd78a7d3745c1"
  end

  resource "perl-net-ssleay" do
    url "https://repo.msys2.org/msys/x86_64/perl-net-ssleay-1.96-2-x86_64.pkg.tar.zst"
    sha256 "63b534ee973831d01b9c9e64fd8891925e9cf0c8923326b63f3c9d4582fee820"
  end

  resource "perl-termreadkey" do
    url "https://repo.msys2.org/msys/x86_64/perl-termreadkey-2.38-9-x86_64.pkg.tar.zst"
    sha256 "584029cb544e78f46d7cc2040d22d144bc26657d0584452d2119dc67f1cf747d"
  end

  resource "perl-timedate" do
    url "https://repo.msys2.org/msys/x86_64/perl-timedate-2.35-2-any.pkg.tar.zst"
    sha256 "ddd3cd5bb07f6c1cc2a6315233e48002d904a492901134d6f1f4139d21f31625"
  end

  resource "perl-try-tiny" do
    url "https://repo.msys2.org/msys/x86_64/perl-try-tiny-0.32-2-any.pkg.tar.zst"
    sha256 "3d36d2b776c6aa8d4313b3b08eea81ad58002783c5f95c4ad8ca718cd84b9c8c"
  end

  resource "perl-uri" do
    url "https://repo.msys2.org/msys/x86_64/perl-uri-5.37-2-any.pkg.tar.zst"
    sha256 "80c66674e48c53b56561fe76d39a739a80fd0796644d535a70a3117bba52add2"
  end

  resource "perl-www-robotrules" do
    url "https://repo.msys2.org/msys/x86_64/perl-www-robotrules-6.03-2-any.pkg.tar.zst"
    sha256 "c4b720d8a7e5973d524de6b53d798db49e6f3697252da1ea68a680ca0861c4d5"
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
    msys2_bin "usr/bin/tig.exe", "tig"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tig -v")
  end
end
