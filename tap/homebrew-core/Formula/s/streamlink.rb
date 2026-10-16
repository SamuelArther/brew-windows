class Streamlink < Formula
  desc "CLI for extracting streams from various websites to a video player"
  homepage "https://streamlink.github.io/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-streamlink-8.6.2-1-any.pkg.tar.zst"
  sha256 "5c46d6f8f5591bedc991864e37048c137b4c00c05d458df4deeab9a6778b6d25"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://files.pythonhosted.org/packages/79/0d/d7875f76e1e87c9978bed557af5d5d43d4adbd495117d363f9dbfd09255c/streamlink-8.6.2.tar.gz"
  end

  resource "mingw-w64-ucrt-x86_64-bzip2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-bzip2-1.0.8-4-any.pkg.tar.zst"
    sha256 "f03a2174034ddd2d96cecd34f617c5f8e2ef86c812b8d2bb3b8875257f2c8bfa"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-expat" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-expat-2.9.0-1-any.pkg.tar.zst"
    sha256 "f52364dc874009316e1ee1fe46ee89784594b80d10cbcc1cc856945d575308e0"
  end

  resource "mingw-w64-ucrt-x86_64-gettext-runtime" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gettext-runtime-1.0-1-any.pkg.tar.zst"
    sha256 "ba693dda4ac375af76ce481ff3a6e7481286546cc7dc6d56c7021dae34084157"
  end

  resource "mingw-w64-ucrt-x86_64-gmp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gmp-6.3.0-2-any.pkg.tar.zst"
    sha256 "e82a75968a556484a50084578238a84eb60fb93e34986fd6695c537975bd39ea"
  end

  resource "mingw-w64-ucrt-x86_64-libb2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libb2-0.98.1-3-any.pkg.tar.zst"
    sha256 "dfb554635119d3c46adb00c9d0c589bb38d1cb0781b10ecf31de07ce388bd0c0"
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

  resource "mingw-w64-ucrt-x86_64-libstdc++" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libstdc++-16.2.0-6-any.pkg.tar.zst"
    sha256 "33c5e58a85a538cf843a8672991cce591f0231d66da3b66a57629712d64be17f"
  end

  resource "mingw-w64-ucrt-x86_64-libsystre" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libsystre-1.0.2-3-any.pkg.tar.zst"
    sha256 "62f564b1395d4862db1599b77f6588c32de5c80936845751cf369b31eee1560b"
  end

  resource "mingw-w64-ucrt-x86_64-libtre" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libtre-0.9.0-2-any.pkg.tar.zst"
    sha256 "d50f22545c50d7c0cd584474bba83f0c7a2cd8aab1aed1ad6c1f9c6452f3c4fc"
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

  resource "mingw-w64-ucrt-x86_64-mpdecimal" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-mpdecimal-4.0.1-3-any.pkg.tar.zst"
    sha256 "abf25c3869ba5e5bc3b69fdeb6cd49b060fc1a64d0d2de10918adeae9630a68d"
  end

  resource "mingw-w64-ucrt-x86_64-ncurses" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-ncurses-6.6-4-any.pkg.tar.zst"
    sha256 "88331fe78470ef2c5e0280652fee5e10b6dcaa35f8037725c359407f2702309c"
  end

  resource "mingw-w64-ucrt-x86_64-openssl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openssl-3.6.5-1-any.pkg.tar.zst"
    sha256 "773e021ebad83f14b5c103db467411e8ff064663e1c7e7f730e2be72f4117064"
  end

  resource "mingw-w64-ucrt-x86_64-pcre2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-pcre2-10.49-1-any.pkg.tar.zst"
    sha256 "63172179db724ac71e2439a7981134c1bcab8049bdddfd349b32b8f76ed52d1e"
  end

  resource "mingw-w64-ucrt-x86_64-python" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-3.14.8-1-any.pkg.tar.zst"
    sha256 "d6542336430039a7cb75f107b713332887665a26a0c9c51e82d8d772c7770b5e"
  end

  resource "mingw-w64-ucrt-x86_64-python-attrs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-attrs-26.1.0-1-any.pkg.tar.zst"
    sha256 "825fd83fad80e79366b26904065d77384aaf2cc4d193e77507a5a096c38de4a2"
  end

  resource "mingw-w64-ucrt-x86_64-python-certifi" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-certifi-2026.7.22-1-any.pkg.tar.zst"
    sha256 "9598f44f2bdce8568eff35b5e7a8168c439d4c0ce597038970a865a16f25cb21"
  end

  resource "mingw-w64-ucrt-x86_64-python-cffi" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-cffi-2.1.1-1-any.pkg.tar.zst"
    sha256 "051bd2f9c9eb7e547b5543665f2d133f0b623ac1a77aa1a81840505f8bb26d6a"
  end

  resource "mingw-w64-ucrt-x86_64-python-charset-normalizer" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-charset-normalizer-3.5.2-1-any.pkg.tar.zst"
    sha256 "fd2b92b6623b86cbcd235148f30145ee0d9387a86cd49a0ab9f8f8be9e6e42a4"
  end

  resource "mingw-w64-ucrt-x86_64-python-h11" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-h11-0.16.0-3-any.pkg.tar.zst"
    sha256 "157bf0c1cc1838b7a9fc1842662bdbda988f8cfec60024b9a0857a0e5079d4ab"
  end

  resource "mingw-w64-ucrt-x86_64-python-idna" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-idna-3.20-1-any.pkg.tar.zst"
    sha256 "fa0074ba72a46ec4fe29737ba9965cd11f60c436c16000b0325605db9f682f7f"
  end

  resource "mingw-w64-ucrt-x86_64-python-isodate" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-isodate-0.7.2-3-any.pkg.tar.zst"
    sha256 "f46c9915e01c206a37a23ced4611535c185fb45c297f0941fa08e4b41a0a1df4"
  end

  resource "mingw-w64-ucrt-x86_64-python-lxml" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-lxml-6.1.3-1-any.pkg.tar.zst"
    sha256 "1598caf1fe6274cb28cdef2782ad27a96b97d90d4703da7be3fbe21aeade9f31"
  end

  resource "mingw-w64-ucrt-x86_64-python-outcome" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-outcome-1.3.0.post0-4-any.pkg.tar.zst"
    sha256 "e2a0e3a24297034020f1ab0589eaaeb12f2f888165b345d9da7c009687e28844"
  end

  resource "mingw-w64-ucrt-x86_64-python-pycountry" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-pycountry-26.2.16-1-any.pkg.tar.zst"
    sha256 "ef1594f922dafab1c9ad366c6c83d87e93e85c7c743cfdb3fa8f8afe5b504eba"
  end

  resource "mingw-w64-ucrt-x86_64-python-pycparser" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-pycparser-3.11-1-any.pkg.tar.zst"
    sha256 "b812787cca682a831a3be10306c5837ff6f50a90e6e6106fc9405ddabe7579e5"
  end

  resource "mingw-w64-ucrt-x86_64-python-pycryptodome" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-pycryptodome-3.24.0-1-any.pkg.tar.zst"
    sha256 "c4494d8f29841877b429b0cb8405224464a23877c0d00bd10209bb7926950f10"
  end

  resource "mingw-w64-ucrt-x86_64-python-pysocks" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-pysocks-1.7.1-4-any.pkg.tar.zst"
    sha256 "4e5448c3e3fb526e6d6d0be1bce555c1906a8a2adcc40129b9820fa470caf123"
  end

  resource "mingw-w64-ucrt-x86_64-python-requests" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-requests-2.34.2-1-any.pkg.tar.zst"
    sha256 "7a449d97dea8622455bd1065d3e81e5c32ad84b85d8ee0894ad5b5e5d4672cf7"
  end

  resource "mingw-w64-ucrt-x86_64-python-sniffio" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-sniffio-1.3.1-4-any.pkg.tar.zst"
    sha256 "04bc72a390ce79d379e69d8a2918d4e3f5e690f9248a0411dd2efa9b0e4ce1b7"
  end

  resource "mingw-w64-ucrt-x86_64-python-sortedcontainers" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-sortedcontainers-2.4.0-8-any.pkg.tar.zst"
    sha256 "83a3ad61cf5093e7668215b1eb0c54247d0dedb723d884f21dee37ef0545cd5f"
  end

  resource "mingw-w64-ucrt-x86_64-python-trio" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-trio-0.34.0-1-any.pkg.tar.zst"
    sha256 "39845ceb7c646952a8855b3c9b3226c4b5477a7c3f0dd7c1071232a0fe61ab66"
  end

  resource "mingw-w64-ucrt-x86_64-python-trio-websocket" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-trio-websocket-0.12.2-3-any.pkg.tar.zst"
    sha256 "9fee452620b4266a9fce5defa7603aa96f5548319028efa1f3e6094485bb3e7f"
  end

  resource "mingw-w64-ucrt-x86_64-python-urllib3" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-urllib3-2.8.0-1-any.pkg.tar.zst"
    sha256 "6310d118efb51e9bd14022aa8ddc600dd32ede5929f8d2f20c8c56474458bbf8"
  end

  resource "mingw-w64-ucrt-x86_64-python-websocket-client" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-websocket-client-1.9.2-1-any.pkg.tar.zst"
    sha256 "e4fe972b6577781ca89b3b60dbf732679942f21bb9a5f6a00e580733c4d6e999"
  end

  resource "mingw-w64-ucrt-x86_64-python-wsproto" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-wsproto-1.2.0-4-any.pkg.tar.zst"
    sha256 "c1b97a01d4cb8e65a02edb19f75fd8181942b9d1fead9a5dd98d90926206d2e6"
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

  def install
    msys2_install
    msys2_bin "ucrt64/bin/streamlink.exe", "streamlink"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/streamlink --version 2>&1")
  end
end
