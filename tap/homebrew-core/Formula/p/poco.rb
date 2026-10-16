class Poco < Formula
  desc "C++ class libraries for building network and internet-based applications"
  homepage "https://pocoproject.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-poco-1.14.2-7-any.pkg.tar.zst"
  sha256 "9ff26f63bdbb66b404f7e9ef71b4c542c70a3453df4f6b2bba95beab36fb1c3d"
  license "BSL-1.0"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://pocoproject.org/releases/"
    regex(%r{href=.*?poco[._-]v?(\d+(?:\.\d+)+\w*)/?["' >]}i)
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

  resource "mingw-w64-ucrt-x86_64-gettext-runtime" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gettext-runtime-1.0-1-any.pkg.tar.zst"
    sha256 "ba693dda4ac375af76ce481ff3a6e7481286546cc7dc6d56c7021dae34084157"
  end

  resource "mingw-w64-ucrt-x86_64-gmp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gmp-6.3.0-2-any.pkg.tar.zst"
    sha256 "e82a75968a556484a50084578238a84eb60fb93e34986fd6695c537975bd39ea"
  end

  resource "mingw-w64-ucrt-x86_64-gnutls" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gnutls-3.8.13-3-any.pkg.tar.zst"
    sha256 "303da3babd11f545fcac647439298e8559951ce6a1086666faf8ad2a1f708be6"
  end

  resource "mingw-w64-ucrt-x86_64-icu" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-icu-78.3-4-any.pkg.tar.zst"
    sha256 "db9282bbd0a2e74c31d66af6bd89eb68d647a09f721eef4c661768588dca4324"
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

  resource "mingw-w64-ucrt-x86_64-libmariadbclient" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libmariadbclient-3.4.9-1-any.pkg.tar.zst"
    sha256 "2f5ae47851903c07b4015a45fbd2a634c6af57d4ed00f6adbe444927eca8bb45"
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

  resource "mingw-w64-ucrt-x86_64-libtasn1" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libtasn1-4.21.0-1-any.pkg.tar.zst"
    sha256 "d098b27834c3b25142b6f5aa9a0ca5731a8e4334c232786406cb149dd8bde2ba"
  end

  resource "mingw-w64-ucrt-x86_64-libunistring" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libunistring-1.4.2-1-any.pkg.tar.zst"
    sha256 "e784273da19bbdcc91cbb510b5e27c3ac021b026140734eefcce5ccbd878bd97"
  end

  resource "mingw-w64-ucrt-x86_64-libutf8proc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libutf8proc-2.11.3-1-any.pkg.tar.zst"
    sha256 "cb4b662db4b3cfa98110ea1441f6f426c7f7ed14a677e761e937d406a93ad32a"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-libxml2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libxml2-2.15.4-1-any.pkg.tar.zst"
    sha256 "ea6b8e7ee24d8674e719f1f0a1b3747a72158ab786dfc5392ccb9f8edd09582a"
  end

  resource "mingw-w64-ucrt-x86_64-lz4" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lz4-1.10.0-1-any.pkg.tar.zst"
    sha256 "00a807df81a4b7e82445178b8625962ed592cf1f0a08a8e04c8b9338c4362638"
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

  resource "mingw-w64-ucrt-x86_64-postgresql" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-postgresql-18.6-4-any.pkg.tar.zst"
    sha256 "45410374cb165108094a49a7d660e57963b804139b4d9ee45a892fc9da09273f"
  end

  resource "mingw-w64-ucrt-x86_64-readline" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-readline-8.3.003-1-any.pkg.tar.zst"
    sha256 "de2423c2e10fcd88272a0ab2f833f6a082cfe613d4c17c2f548cc50a5d2190c4"
  end

  resource "mingw-w64-ucrt-x86_64-sqlite3" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-sqlite3-3.54.0-1-any.pkg.tar.zst"
    sha256 "fc7100490316733bbb96d5633d305a5d6c579200c321969cfa21d5a128055e65"
  end

  resource "mingw-w64-ucrt-x86_64-tcl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tcl-8.6.18-1-any.pkg.tar.zst"
    sha256 "0c3a50ea5be8c5190e58a1fdc46c09f8113ad1b855745875646b7a78a02215df"
  end

  resource "mingw-w64-ucrt-x86_64-termcap" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-termcap-1.3.1-8-any.pkg.tar.zst"
    sha256 "c7891143e252568d8d46df72b3aefea932f4166771e17f22317218d7dd2af23c"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  resource "mingw-w64-ucrt-x86_64-wineditline" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-wineditline-2.208-1-any.pkg.tar.zst"
    sha256 "b07b34de1669e16cfd6743691914f5a66ad4013500bbf0e4c7a7644046ae7b1c"
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
    msys2_bin "ucrt64/bin/cpspc.exe", "cpspc"
    msys2_bin "ucrt64/bin/f2cpsp.exe", "f2cpsp"
    msys2_bin "ucrt64/bin/poco-arc.exe", "poco-arc"
  end

  test do
    assert_path_exists bin/"cpspc.exe"
  end
end
