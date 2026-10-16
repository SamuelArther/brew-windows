class Netcdf < Formula
  desc "Libraries and data formats for array-oriented scientific data"
  homepage "https://www.unidata.ucar.edu/software/netcdf/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-netcdf-4.10.1-2-any.pkg.tar.zst"
  sha256 "03f5ce848227787515248f682dbf61cb1930ffe14f5fbe246bf12c512c0d8066"
  license "BSD-3-Clause"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/Unidata/netcdf-c/archive/refs/tags/v4.10.1.tar.gz"
    regex(/^(?:netcdf[._-])?v?(\d+(?:\.\d+)+)$/i)
  end

  resource "mingw-w64-ucrt-x86_64-aws-c-auth" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-auth-1.0.0-1-any.pkg.tar.zst"
    sha256 "465d9e496d69b57b2a19e966fe60832f503a37fdff8ea89743fa09cccf2bc578"
  end

  resource "mingw-w64-ucrt-x86_64-aws-c-cal" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-cal-1.0.0-1-any.pkg.tar.zst"
    sha256 "07f61cf307c232ef7bef0a132fec05711c8d1facfecb161d6f570a243aeb91c9"
  end

  resource "mingw-w64-ucrt-x86_64-aws-c-common" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-common-1.0.2-1-any.pkg.tar.zst"
    sha256 "d7312c06681a0a7762f92ea0dedfdbb16efe4a9ac20ea2e43d095deca7f2a0c5"
  end

  resource "mingw-w64-ucrt-x86_64-aws-c-compression" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-compression-1.0.0-1-any.pkg.tar.zst"
    sha256 "2516f6ab54d19c25e698a715e74f677a0c60379d6d31e7ab3e44132ddb6f7d57"
  end

  resource "mingw-w64-ucrt-x86_64-aws-c-event-stream" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-event-stream-1.0.0-1-any.pkg.tar.zst"
    sha256 "546d1f36e78c90b02e90f953f42dbb063f4674bead2dddc7615b07b40980c549"
  end

  resource "mingw-w64-ucrt-x86_64-aws-c-http" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-http-1.0.0-1-any.pkg.tar.zst"
    sha256 "51a61a36fb65283bf3b159d1cf49a47114955d0d8ee2f05d8f253579d5bf0fd4"
  end

  resource "mingw-w64-ucrt-x86_64-aws-c-io" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-io-1.0.0-1-any.pkg.tar.zst"
    sha256 "f44695d9b9a4592e570d362f65d82f516f92a5e375788afd068e02cf80e7f082"
  end

  resource "mingw-w64-ucrt-x86_64-aws-c-mqtt" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-mqtt-1.0.0-1-any.pkg.tar.zst"
    sha256 "0175e4ce24c222f54795b6598182b3171016124b8810536ecb0360ca91532c8b"
  end

  resource "mingw-w64-ucrt-x86_64-aws-c-s3" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-s3-1.3.0-1-any.pkg.tar.zst"
    sha256 "5039dae6f4f360fb87196b077a202118b10b6a70b45418a306ac141ad72541e7"
  end

  resource "mingw-w64-ucrt-x86_64-aws-c-sdkutils" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-sdkutils-1.0.0-1-any.pkg.tar.zst"
    sha256 "c5d0ab458a3891460d83ce1546511a965b44424c619779dd4e937c9bf9d08c3d"
  end

  resource "mingw-w64-ucrt-x86_64-aws-checksums" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-checksums-1.0.1-1-any.pkg.tar.zst"
    sha256 "339c11e15703e955ad86cb4bd69d1d4fcbfeb93d89daee90107f8c272a17c374"
  end

  resource "mingw-w64-ucrt-x86_64-aws-crt-cpp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-crt-cpp-0.43.7-1-any.pkg.tar.zst"
    sha256 "5b9612c8c9ae7d971daa467f3be1d8d4917ae4e72d536987b5c21b778df5458d"
  end

  resource "mingw-w64-ucrt-x86_64-aws-sdk-cpp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-sdk-cpp-1.11.893-1-any.pkg.tar.zst"
    sha256 "f2dc1b6d84eabb300e88e324ff9a5fe44a6d8c1aac20f3d6b4a380a5df57cae6"
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

  resource "mingw-w64-ucrt-x86_64-hdf5" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-hdf5-2.2.0-2-any.pkg.tar.zst"
    sha256 "ec34643003bdc6ae7e8416ab5549afb90c960bdc601fab7fdcfec1ee04f8e8ba"
  end

  resource "mingw-w64-ucrt-x86_64-libaec" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libaec-1.1.7-2-any.pkg.tar.zst"
    sha256 "b30e0fce8bff0f782e18e216298bf8f27d9dc3f5cf49b7f319b4e8a359658572"
  end

  resource "mingw-w64-ucrt-x86_64-libffi" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libffi-3.8.0-1-any.pkg.tar.zst"
    sha256 "99ad12f4ecfa00a889ef9c5c0188592368a5bac109d0c508ce4e04f29ea95a76"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libgfortran" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgfortran-16.2.0-6-any.pkg.tar.zst"
    sha256 "34406a0c721e44655c3971ce131efd81652d5f6fa5381eb5700b288bc9a780ab"
  end

  resource "mingw-w64-ucrt-x86_64-libiconv" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libiconv-1.19-1-any.pkg.tar.zst"
    sha256 "9a500f38c2b91808741c62fae746b3e9110b33a1ecf5c30fa0c66dbedddf7e16"
  end

  resource "mingw-w64-ucrt-x86_64-libidn2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libidn2-2.3.8-4-any.pkg.tar.zst"
    sha256 "d5f4b5e9d99ff7b2a986d1dc0c73da2bcd93ba68462be7b1debbd6ef76371f57"
  end

  resource "mingw-w64-ucrt-x86_64-libpsl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libpsl-0.21.5-3-any.pkg.tar.zst"
    sha256 "d96e0bb50ac3f8b6e44a39ea8de8d186f7b08f6938b0676eb69a7ba1f127bcf4"
  end

  resource "mingw-w64-ucrt-x86_64-libquadmath" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libquadmath-16.2.0-6-any.pkg.tar.zst"
    sha256 "7b553c0b6dd9cf2f676c0c6812aa359495940dbdd7347f47be1dbd75d29cd611"
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

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-libxml2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libxml2-2.15.4-1-any.pkg.tar.zst"
    sha256 "ea6b8e7ee24d8674e719f1f0a1b3747a72158ab786dfc5392ccb9f8edd09582a"
  end

  resource "mingw-w64-ucrt-x86_64-libzip" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libzip-1.12-1-any.pkg.tar.zst"
    sha256 "a7e615b5bd0b1bc76b49048225ad446a9b255f1e300399331209167b8d53124e"
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

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  resource "mingw-w64-ucrt-x86_64-xz" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-xz-5.8.4-1-any.pkg.tar.zst"
    sha256 "a9d771f816d8c0c5bde152ffb27a687ede91079b8e77e5b8297efa3b5a835934"
  end

  resource "mingw-w64-ucrt-x86_64-zlib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-1.3.2-2-any.pkg.tar.zst"
    sha256 "841401182976d2f9e17e5c0ebaac51f2a8014140ea53d67625e91c8fb3c85ea0"
  end

  resource "mingw-w64-ucrt-x86_64-zlib-ng" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-ng-2.3.3-2-any.pkg.tar.zst"
    sha256 "1d7dda169b6833a8f5d6cbbd2c630e2b88165fa47ed3de709b732fa3259023b7"
  end

  resource "mingw-w64-ucrt-x86_64-zstd" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zstd-1.5.7-2-any.pkg.tar.zst"
    sha256 "dbdb8427280046a2b41697780aa4c52983b708082b0da4755951dc3bea96ca89"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/nccopy.exe", "nccopy"
    msys2_bin "ucrt64/bin/ncdump.exe", "ncdump"
    msys2_bin "ucrt64/bin/ncgen.exe", "ncgen"
    msys2_bin "ucrt64/bin/ncgen3.exe", "ncgen3"
  end

  test do
    assert_path_exists bin/"nccopy.exe"
  end
end
