class Poppler < Formula
  desc "PDF rendering library (based on the xpdf-3.0 code base)"
  homepage "https://poppler.freedesktop.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-poppler-26.08.0-1-any.pkg.tar.zst"
  sha256 "77a814a61a8578a6f4300cdc052635473371631df72d545bb193227e8a8d4882"
  license any_of: ["GPL-2.0-only", "GPL-3.0-only"] # see README-XPDF

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url :homepage
    regex(/href=.*?poppler[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  resource "mingw-w64-ucrt-x86_64-brotli" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-brotli-1.2.0-1-any.pkg.tar.zst"
    sha256 "9cc89665496ea504751476eafba15c71c554aaf3004babc2004f7c07ffd60514"
  end

  resource "mingw-w64-ucrt-x86_64-bzip2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-bzip2-1.0.8-4-any.pkg.tar.zst"
    sha256 "f03a2174034ddd2d96cecd34f617c5f8e2ef86c812b8d2bb3b8875257f2c8bfa"
  end

  resource "mingw-w64-ucrt-x86_64-c-ares" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-c-ares-1.34.8-1-any.pkg.tar.zst"
    sha256 "2313d531bc9dc7aae6be3ce0f8ace516bb04191c4f3f62c78c7dfd246d2a9ba6"
  end

  resource "mingw-w64-ucrt-x86_64-ca-certificates" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-ca-certificates-20260816-1-any.pkg.tar.zst"
    sha256 "123fac39b85ac36364d5ff9c1411cb448f9581fd12940af83adcd12a8eab9f55"
  end

  resource "mingw-w64-ucrt-x86_64-cairo" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cairo-1.18.6-2-any.pkg.tar.zst"
    sha256 "021c0a4d1df463d08d0cac170818a7cbaccaa5b09ac6edc7948e461cb5a46e85"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-curl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-curl-8.23.0-1-any.pkg.tar.zst"
    sha256 "cb1b4e7c0aa8e6e39ac88af83f083d36fb9ea4c50377e795fdb7c3e97ac9fda5"
  end

  resource "mingw-w64-ucrt-x86_64-expat" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-expat-2.9.0-1-any.pkg.tar.zst"
    sha256 "f52364dc874009316e1ee1fe46ee89784594b80d10cbcc1cc856945d575308e0"
  end

  resource "mingw-w64-ucrt-x86_64-fontconfig" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-fontconfig-2.18.3-1-any.pkg.tar.zst"
    sha256 "d0b364f3ee37e3b6f6c136688ce8fa909bb6d8a69df7e08de1aa40870d9e1d60"
  end

  resource "mingw-w64-ucrt-x86_64-freetype" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-freetype-2.15.0-1-any.pkg.tar.zst"
    sha256 "a9a691f71d1a553099c54ca666e611abfb2b4ff08dae24f2a5af72e22a7e5feb"
  end

  resource "mingw-w64-ucrt-x86_64-gettext-runtime" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gettext-runtime-1.0-1-any.pkg.tar.zst"
    sha256 "ba693dda4ac375af76ce481ff3a6e7481286546cc7dc6d56c7021dae34084157"
  end

  resource "mingw-w64-ucrt-x86_64-giflib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-giflib-6.1.3-1-any.pkg.tar.zst"
    sha256 "9e7ac8feb0e2af6e0fce3c8ead1bd7bcaf018661d08033858c1e2657a04f6f1a"
  end

  resource "mingw-w64-ucrt-x86_64-glib2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-glib2-2.90.1-1-any.pkg.tar.zst"
    sha256 "9a7826349279bcca0c935b01880d812be9de9609aef2205be68fd7a6128c1695"
  end

  resource "mingw-w64-ucrt-x86_64-gmp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gmp-6.3.0-2-any.pkg.tar.zst"
    sha256 "e82a75968a556484a50084578238a84eb60fb93e34986fd6695c537975bd39ea"
  end

  resource "mingw-w64-ucrt-x86_64-gnutls" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gnutls-3.8.13-3-any.pkg.tar.zst"
    sha256 "303da3babd11f545fcac647439298e8559951ce6a1086666faf8ad2a1f708be6"
  end

  resource "mingw-w64-ucrt-x86_64-graphite2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-graphite2-1.3.15-1-any.pkg.tar.zst"
    sha256 "34ca1f88bff10ef182a6b1c8e5622d5029f13b05748e361af13b552b422ea849"
  end

  resource "mingw-w64-ucrt-x86_64-harfbuzz" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-harfbuzz-14.6.0-1-any.pkg.tar.zst"
    sha256 "8781d486c864bf91f482768cc5bd7267e49331636583d9f855af3974e6378e69"
  end

  resource "mingw-w64-ucrt-x86_64-jbigkit" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-jbigkit-2.1-6-any.pkg.tar.zst"
    sha256 "9e237d1041350e7436294cfa13d4bd3d1606d1f2a32ade2eb2958b4d4e79b7ad"
  end

  resource "mingw-w64-ucrt-x86_64-lcms2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lcms2-2.19.1-1-any.pkg.tar.zst"
    sha256 "144518cc80679f7bcc1d31b5dd7ec92e8737a1d8162014e30c8c1c181f47a156"
  end

  resource "mingw-w64-ucrt-x86_64-lerc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lerc-4.1.1-1-any.pkg.tar.zst"
    sha256 "c0afbf4832488d67030c6f313349cd814946c9a7bc3ae9fd54524825c9ca055d"
  end

  resource "mingw-w64-ucrt-x86_64-libb2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libb2-0.98.1-3-any.pkg.tar.zst"
    sha256 "dfb554635119d3c46adb00c9d0c589bb38d1cb0781b10ecf31de07ce388bd0c0"
  end

  resource "mingw-w64-ucrt-x86_64-libdeflate" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libdeflate-1.26-1-any.pkg.tar.zst"
    sha256 "17720c77bd01641a3554801cafd71d96fb7fff7427f4cc20ac9f36efb8cb3da0"
  end

  resource "mingw-w64-ucrt-x86_64-libffi" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libffi-3.8.0-1-any.pkg.tar.zst"
    sha256 "99ad12f4ecfa00a889ef9c5c0188592368a5bac109d0c508ce4e04f29ea95a76"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libiconv" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libiconv-1.19-1-any.pkg.tar.zst"
    sha256 "9a500f38c2b91808741c62fae746b3e9110b33a1ecf5c30fa0c66dbedddf7e16"
  end

  resource "mingw-w64-ucrt-x86_64-libidn2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libidn2-2.3.8-4-any.pkg.tar.zst"
    sha256 "d5f4b5e9d99ff7b2a986d1dc0c73da2bcd93ba68462be7b1debbd6ef76371f57"
  end

  resource "mingw-w64-ucrt-x86_64-libjpeg-turbo" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libjpeg-turbo-3.2.0-1-any.pkg.tar.zst"
    sha256 "7c001d00df557cfee969ad43d37b9c31ae22869e8aac4091a89dc3ae6d95e11d"
  end

  resource "mingw-w64-ucrt-x86_64-libpng" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libpng-1.6.59-1-any.pkg.tar.zst"
    sha256 "7f8448e06eceba8f8ce130390addea49f767bdf8f6b7e7eede8ca6d595e3d3a8"
  end

  resource "mingw-w64-ucrt-x86_64-libpsl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libpsl-0.21.5-3-any.pkg.tar.zst"
    sha256 "d96e0bb50ac3f8b6e44a39ea8de8d186f7b08f6938b0676eb69a7ba1f127bcf4"
  end

  resource "mingw-w64-ucrt-x86_64-libssh2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libssh2-1.11.1-2-any.pkg.tar.zst"
    sha256 "0e91316dfa4979dcabbfa9e71528ec9882ece619ce549cb25312089b4754fa65"
  end

  resource "mingw-w64-ucrt-x86_64-libstdc++" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libstdc++-16.2.0-6-any.pkg.tar.zst"
    sha256 "33c5e58a85a538cf843a8672991cce591f0231d66da3b66a57629712d64be17f"
  end

  resource "mingw-w64-ucrt-x86_64-libsystre" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libsystre-1.0.2-3-any.pkg.tar.zst"
    sha256 "62f564b1395d4862db1599b77f6588c32de5c80936845751cf369b31eee1560b"
  end

  resource "mingw-w64-ucrt-x86_64-libtasn1" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libtasn1-4.21.0-1-any.pkg.tar.zst"
    sha256 "d098b27834c3b25142b6f5aa9a0ca5731a8e4334c232786406cb149dd8bde2ba"
  end

  resource "mingw-w64-ucrt-x86_64-libtiff" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libtiff-4.7.2-1-any.pkg.tar.zst"
    sha256 "76904b28133fc3cc0a0e518846c98d3d2166fac70ed9c6184dfe5ef145e5f279"
  end

  resource "mingw-w64-ucrt-x86_64-libtre" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libtre-0.9.0-2-any.pkg.tar.zst"
    sha256 "d50f22545c50d7c0cd584474bba83f0c7a2cd8aab1aed1ad6c1f9c6452f3c4fc"
  end

  resource "mingw-w64-ucrt-x86_64-libunistring" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libunistring-1.4.2-1-any.pkg.tar.zst"
    sha256 "e784273da19bbdcc91cbb510b5e27c3ac021b026140734eefcce5ccbd878bd97"
  end

  resource "mingw-w64-ucrt-x86_64-libwebp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwebp-1.6.0-1-any.pkg.tar.zst"
    sha256 "34ff8396c403d484c8777ad49ad0fc06dfb94fadf6f14bcf84d50c45bc6fd17e"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-lzo2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lzo2-2.10-3-any.pkg.tar.zst"
    sha256 "717c8065996bea51543518a0d3ab46837bf9a990c41886348bd2fa9484ea23f3"
  end

  resource "mingw-w64-ucrt-x86_64-mpdecimal" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-mpdecimal-4.0.1-3-any.pkg.tar.zst"
    sha256 "abf25c3869ba5e5bc3b69fdeb6cd49b060fc1a64d0d2de10918adeae9630a68d"
  end

  resource "mingw-w64-ucrt-x86_64-ncurses" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-ncurses-6.6-4-any.pkg.tar.zst"
    sha256 "88331fe78470ef2c5e0280652fee5e10b6dcaa35f8037725c359407f2702309c"
  end

  resource "mingw-w64-ucrt-x86_64-nettle" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-nettle-4.0-1-any.pkg.tar.zst"
    sha256 "47b49f1723ef2c75ff3042b6e463910c47cdffa3342e175b6409fb9d630e64ed"
  end

  resource "mingw-w64-ucrt-x86_64-nghttp2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-nghttp2-1.70.0-1-any.pkg.tar.zst"
    sha256 "39baf792ff3855c1d656514eaef648fab20de80614517fc15f57a57fa103cc20"
  end

  resource "mingw-w64-ucrt-x86_64-nghttp3" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-nghttp3-1.18.0-1-any.pkg.tar.zst"
    sha256 "06e51ea288e5fdd511c753038979772ec22ec71504dac61f912c63a6e586ba80"
  end

  resource "mingw-w64-ucrt-x86_64-ngtcp2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-ngtcp2-1.25.0-1-any.pkg.tar.zst"
    sha256 "cce230d80a05cd0a1ec38fa5d76c0445c9bb725c65c2136be3d23e6dc218a89e"
  end

  resource "mingw-w64-ucrt-x86_64-nspr" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-nspr-4.40-1-any.pkg.tar.zst"
    sha256 "35f0f60025cb0d20039f3d2c59df744a9308fe264c4606d49685e87efce5984a"
  end

  resource "mingw-w64-ucrt-x86_64-nss" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-nss-3.131-1-any.pkg.tar.zst"
    sha256 "01da502b6ccc9ce4dfbd251baa6532328a21aedbf22328f208e76327653047c4"
  end

  resource "mingw-w64-ucrt-x86_64-openjpeg2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openjpeg2-2.5.4-2-any.pkg.tar.zst"
    sha256 "2a4c148052b6f8e47abe5e98b29fd8942e2e3c17722ff00f172b1630897a10b9"
  end

  resource "mingw-w64-ucrt-x86_64-openssl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openssl-3.6.5-1-any.pkg.tar.zst"
    sha256 "773e021ebad83f14b5c103db467411e8ff064663e1c7e7f730e2be72f4117064"
  end

  resource "mingw-w64-ucrt-x86_64-p11-kit" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-p11-kit-0.26.5-1-any.pkg.tar.zst"
    sha256 "6e1645f0afa669f3e3325abb891d79c58dacf0bb5f9bb1ea719d22da1d754e62"
  end

  resource "mingw-w64-ucrt-x86_64-pcre2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-pcre2-10.49-1-any.pkg.tar.zst"
    sha256 "63172179db724ac71e2439a7981134c1bcab8049bdddfd349b32b8f76ed52d1e"
  end

  resource "mingw-w64-ucrt-x86_64-pixman" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-pixman-0.46.4-3-any.pkg.tar.zst"
    sha256 "4c01b986563d2b5f7ad6fcec2d0ec9915d2f242ddac5eacddc19971964241cc5"
  end

  resource "mingw-w64-ucrt-x86_64-poppler-data" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-poppler-data-0.4.12-1-any.pkg.tar.zst"
    sha256 "fc5f917e80b2de52b335b6ee99c3e047af08b65e84c94cdb1ec22e7f41d93911"
  end

  resource "mingw-w64-ucrt-x86_64-python" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-3.14.8-1-any.pkg.tar.zst"
    sha256 "d6542336430039a7cb75f107b713332887665a26a0c9c51e82d8d772c7770b5e"
  end

  resource "mingw-w64-ucrt-x86_64-python-packaging" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-packaging-26.3-1-any.pkg.tar.zst"
    sha256 "b90d1451cfbf015a5334409bd9edf9c61bc0f7b78cac19400b0f92c9279fbaa7"
  end

  resource "mingw-w64-ucrt-x86_64-sqlite3" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-sqlite3-3.54.0-1-any.pkg.tar.zst"
    sha256 "fc7100490316733bbb96d5633d305a5d6c579200c321969cfa21d5a128055e65"
  end

  resource "mingw-w64-ucrt-x86_64-tcl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tcl-8.6.18-1-any.pkg.tar.zst"
    sha256 "0c3a50ea5be8c5190e58a1fdc46c09f8113ad1b855745875646b7a78a02215df"
  end

  resource "mingw-w64-ucrt-x86_64-tk" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tk-8.6.18-1-any.pkg.tar.zst"
    sha256 "bfa011886032962833ddc93ae9b508798ec56625b9093cc6d0ff5c95507c79c6"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  resource "mingw-w64-ucrt-x86_64-wineditline" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-wineditline-2.208-1-any.pkg.tar.zst"
    sha256 "b07b34de1669e16cfd6743691914f5a66ad4013500bbf0e4c7a7644046ae7b1c"
  end

  resource "mingw-w64-ucrt-x86_64-xz" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-xz-5.8.4-1-any.pkg.tar.zst"
    sha256 "a9d771f816d8c0c5bde152ffb27a687ede91079b8e77e5b8297efa3b5a835934"
  end

  resource "mingw-w64-ucrt-x86_64-zlib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-1.3.2-2-any.pkg.tar.zst"
    sha256 "841401182976d2f9e17e5c0ebaac51f2a8014140ea53d67625e91c8fb3c85ea0"
  end

  resource "mingw-w64-ucrt-x86_64-zstd" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zstd-1.5.7-2-any.pkg.tar.zst"
    sha256 "dbdb8427280046a2b41697780aa4c52983b708082b0da4755951dc3bea96ca89"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/pdfattach.exe", "pdfattach"
    msys2_bin "ucrt64/bin/pdfdetach.exe", "pdfdetach"
    msys2_bin "ucrt64/bin/pdffonts.exe", "pdffonts"
    msys2_bin "ucrt64/bin/pdfimages.exe", "pdfimages"
    msys2_bin "ucrt64/bin/pdfinfo.exe", "pdfinfo"
    msys2_bin "ucrt64/bin/pdfseparate.exe", "pdfseparate"
    msys2_bin "ucrt64/bin/pdfsig.exe", "pdfsig"
    msys2_bin "ucrt64/bin/pdftocairo.exe", "pdftocairo"
    msys2_bin "ucrt64/bin/pdftohtml.exe", "pdftohtml"
    msys2_bin "ucrt64/bin/pdftoppm.exe", "pdftoppm"
    msys2_bin "ucrt64/bin/pdftops.exe", "pdftops"
    msys2_bin "ucrt64/bin/pdftotext.exe", "pdftotext"
    msys2_bin "ucrt64/bin/pdfunite.exe", "pdfunite"
  end

  test do
    assert_path_exists bin/"pdfattach.exe"
  end
end
