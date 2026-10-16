class Img2pdf < Formula
  desc "Convert images to PDF via direct JPEG inclusion"
  homepage "https://gitlab.mister-muffin.de/josch/img2pdf"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-img2pdf-0.6.3-1-any.pkg.tar.zst"
  sha256 "2afb10ed4a769b6ce74e5afbdec53dec77150174ba5a6c78b98cf2009827a300"
  license "LGPL-3.0-or-later"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://files.pythonhosted.org/packages/8e/97/ca44c467131b93fda82d2a2f21b738c8bcf63b5259e3b8250e928b8dd52a/img2pdf-0.6.3.tar.gz"
  end

  resource "mingw-w64-ucrt-x86_64-aom" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aom-3.15.2-1-any.pkg.tar.zst"
    sha256 "e81f01d419f262e31275771fad18f93617f7b321deeb416e5ac39d70f295b286"
  end

  resource "mingw-w64-ucrt-x86_64-brotli" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-brotli-1.2.0-1-any.pkg.tar.zst"
    sha256 "9cc89665496ea504751476eafba15c71c554aaf3004babc2004f7c07ffd60514"
  end

  resource "mingw-w64-ucrt-x86_64-bzip2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-bzip2-1.0.8-4-any.pkg.tar.zst"
    sha256 "f03a2174034ddd2d96cecd34f617c5f8e2ef86c812b8d2bb3b8875257f2c8bfa"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-dav1d" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-dav1d-1.5.4-1-any.pkg.tar.zst"
    sha256 "ccf26c6f29fd36f5692d57fd5e0fe3becec492e0b875fd5e04f47a9582eb8b37"
  end

  resource "mingw-w64-ucrt-x86_64-expat" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-expat-2.9.0-1-any.pkg.tar.zst"
    sha256 "f52364dc874009316e1ee1fe46ee89784594b80d10cbcc1cc856945d575308e0"
  end

  resource "mingw-w64-ucrt-x86_64-freetype" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-freetype-2.15.0-1-any.pkg.tar.zst"
    sha256 "a9a691f71d1a553099c54ca666e611abfb2b4ff08dae24f2a5af72e22a7e5feb"
  end

  resource "mingw-w64-ucrt-x86_64-fribidi" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-fribidi-1.0.17-1-any.pkg.tar.zst"
    sha256 "3581ee03cf83a9bb0e98bf688c4f532a29553500e317532d90746de295b0f8a8"
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

  resource "mingw-w64-ucrt-x86_64-libavif" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libavif-1.4.2-1-any.pkg.tar.zst"
    sha256 "9dea354b257d2e593c82c88390a178be547faaaa06f366070b4f8abc784277e6"
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

  resource "mingw-w64-ucrt-x86_64-libimagequant" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libimagequant-4.4.1-1-any.pkg.tar.zst"
    sha256 "8966b692f5cc1b7c2f97ca045075334d4972eefcb03832b6723e1bbe8e5f79e7"
  end

  resource "mingw-w64-ucrt-x86_64-libjpeg-turbo" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libjpeg-turbo-3.2.0-1-any.pkg.tar.zst"
    sha256 "7c001d00df557cfee969ad43d37b9c31ae22869e8aac4091a89dc3ae6d95e11d"
  end

  resource "mingw-w64-ucrt-x86_64-libpng" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libpng-1.6.59-1-any.pkg.tar.zst"
    sha256 "7f8448e06eceba8f8ce130390addea49f767bdf8f6b7e7eede8ca6d595e3d3a8"
  end

  resource "mingw-w64-ucrt-x86_64-libraqm" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libraqm-0.11.0-1-any.pkg.tar.zst"
    sha256 "a4b66d786fb57d291a083209197f8e324f5e88b3332e889926e64ba521c7e279"
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

  resource "mingw-w64-ucrt-x86_64-libxml2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libxml2-2.15.4-1-any.pkg.tar.zst"
    sha256 "ea6b8e7ee24d8674e719f1f0a1b3747a72158ab786dfc5392ccb9f8edd09582a"
  end

  resource "mingw-w64-ucrt-x86_64-libxslt" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libxslt-1.1.45-1-any.pkg.tar.zst"
    sha256 "20fae8833681bf783b13df4db3bb3811cc0dc69311a8e5e10c26ba150a47826e"
  end

  resource "mingw-w64-ucrt-x86_64-libyuv" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libyuv-1924.r2921.644251f25-2-any.pkg.tar.zst"
    sha256 "3016e0f97bf38313bb336447739f636c8622fc0ca2236fe48e3f6cd662ef56cb"
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

  resource "mingw-w64-ucrt-x86_64-python" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-3.14.8-1-any.pkg.tar.zst"
    sha256 "d6542336430039a7cb75f107b713332887665a26a0c9c51e82d8d772c7770b5e"
  end

  resource "mingw-w64-ucrt-x86_64-python-deprecated" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-deprecated-3.0.0-1-any.pkg.tar.zst"
    sha256 "661f0a1ae781640c4f43b7551cc877be574418b6e8e88900f84ecf47b595c71c"
  end

  resource "mingw-w64-ucrt-x86_64-python-lxml" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-lxml-6.1.3-1-any.pkg.tar.zst"
    sha256 "1598caf1fe6274cb28cdef2782ad27a96b97d90d4703da7be3fbe21aeade9f31"
  end

  resource "mingw-w64-ucrt-x86_64-python-packaging" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-packaging-26.3-1-any.pkg.tar.zst"
    sha256 "b90d1451cfbf015a5334409bd9edf9c61bc0f7b78cac19400b0f92c9279fbaa7"
  end

  resource "mingw-w64-ucrt-x86_64-python-pikepdf" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-pikepdf-10.5.1-4-any.pkg.tar.zst"
    sha256 "af610702e488b08cc5d910d87a350257f5cf602cb8226196fe79b126a3476eac"
  end

  resource "mingw-w64-ucrt-x86_64-python-pillow" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-pillow-12.3.0-1-any.pkg.tar.zst"
    sha256 "cec2ce10f2f6bb3df8b90d22af67c96446ada67a382c858e517f9d794c25a696"
  end

  resource "mingw-w64-ucrt-x86_64-python-wrapt" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-wrapt-2.5.1-1-any.pkg.tar.zst"
    sha256 "5ee091526f06db772ada3756f95b8c099ae9d33c60ae9da58c159c297b801307"
  end

  resource "mingw-w64-ucrt-x86_64-qpdf" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-qpdf-12.3.2-1-any.pkg.tar.zst"
    sha256 "99e4bb0adcf6da26ea571d672273fd9bc040e555e6ca95a2f340e7ddb2910887"
  end

  resource "mingw-w64-ucrt-x86_64-rav1e" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-rav1e-0.8.1-1-any.pkg.tar.zst"
    sha256 "00a03a734f96f66405ad79bae2ffb1c5cf2b050ef90a4ffa508ddbfdb64c0fb1"
  end

  resource "mingw-w64-ucrt-x86_64-sqlite3" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-sqlite3-3.54.0-1-any.pkg.tar.zst"
    sha256 "fc7100490316733bbb96d5633d305a5d6c579200c321969cfa21d5a128055e65"
  end

  resource "mingw-w64-ucrt-x86_64-svt-av1" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-svt-av1-4.2.0-1-any.pkg.tar.zst"
    sha256 "87c97a68bab7dd67413ea0cae024c3d2e4bdc2f96fccf38365304642b1b44ee5"
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
    msys2_bin "ucrt64/bin/img2pdf-gui.exe", "img2pdf-gui"
    msys2_bin "ucrt64/bin/img2pdf.exe", "img2pdf"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/img2pdf --version 2>&1")
  end
end
