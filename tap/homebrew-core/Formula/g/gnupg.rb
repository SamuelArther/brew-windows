class Gnupg < Formula
  desc "GNU Privacy Guard (OpenPGP)"
  homepage "https://gnupg.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gnupg-2.4.9-1-any.pkg.tar.zst"
  sha256 "5c6e51b161f1832a10937c3a962a42724cbed9c10b4d16df324d91851c64d745"
  license "GPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url :homepage
    regex(/The current version of GnuPG is v?(\d+(?:\.\d+)+)\. /i)
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

  resource "mingw-w64-ucrt-x86_64-libassuan" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libassuan-2.5.7-2-any.pkg.tar.zst"
    sha256 "fcca8e84b72910a3aa56053e706e2b1efc03df826a26bb6eab1c321991db6826"
  end

  resource "mingw-w64-ucrt-x86_64-libffi" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libffi-3.8.0-1-any.pkg.tar.zst"
    sha256 "99ad12f4ecfa00a889ef9c5c0188592368a5bac109d0c508ce4e04f29ea95a76"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libgcrypt" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcrypt-1.12.4-1-any.pkg.tar.zst"
    sha256 "585121f734c7c00b70274a4cec36c9b7acbfde2402da584a6373f292e2b37fa6"
  end

  resource "mingw-w64-ucrt-x86_64-libgpg-error" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgpg-error-1.61-1-any.pkg.tar.zst"
    sha256 "ca3045875ddae643e6aa927406b8be49670bcdbf31e7c35651bbacf555e95421"
  end

  resource "mingw-w64-ucrt-x86_64-libiconv" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libiconv-1.19-1-any.pkg.tar.zst"
    sha256 "9a500f38c2b91808741c62fae746b3e9110b33a1ecf5c30fa0c66dbedddf7e16"
  end

  resource "mingw-w64-ucrt-x86_64-libidn2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libidn2-2.3.8-4-any.pkg.tar.zst"
    sha256 "d5f4b5e9d99ff7b2a986d1dc0c73da2bcd93ba68462be7b1debbd6ef76371f57"
  end

  resource "mingw-w64-ucrt-x86_64-libksba" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libksba-1.6.8-1-any.pkg.tar.zst"
    sha256 "2fdd5666c9495981614009af931317c18751b8afe7b0c1344cec33e4b875f98f"
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

  resource "mingw-w64-ucrt-x86_64-libtre" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libtre-0.9.0-2-any.pkg.tar.zst"
    sha256 "d50f22545c50d7c0cd584474bba83f0c7a2cd8aab1aed1ad6c1f9c6452f3c4fc"
  end

  resource "mingw-w64-ucrt-x86_64-libunistring" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libunistring-1.4.2-1-any.pkg.tar.zst"
    sha256 "e784273da19bbdcc91cbb510b5e27c3ac021b026140734eefcce5ccbd878bd97"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
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

  resource "mingw-w64-ucrt-x86_64-npth" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-npth-1.8-2-any.pkg.tar.zst"
    sha256 "cc0974ef7bbe32816aa0c7247888f9460e7fcfb91edeca66816b90124e285ef8"
  end

  resource "mingw-w64-ucrt-x86_64-openssl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openssl-3.6.5-1-any.pkg.tar.zst"
    sha256 "773e021ebad83f14b5c103db467411e8ff064663e1c7e7f730e2be72f4117064"
  end

  resource "mingw-w64-ucrt-x86_64-p11-kit" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-p11-kit-0.26.5-1-any.pkg.tar.zst"
    sha256 "6e1645f0afa669f3e3325abb891d79c58dacf0bb5f9bb1ea719d22da1d754e62"
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
    msys2_bin "ucrt64/bin/dirmngr-client.exe", "dirmngr-client"
    msys2_bin "ucrt64/bin/dirmngr.exe", "dirmngr"
    msys2_bin "ucrt64/bin/dirmngr_ldap.exe", "dirmngr_ldap"
    msys2_bin "ucrt64/bin/gpg-agent.exe", "gpg-agent"
    msys2_bin "ucrt64/bin/gpg-card.exe", "gpg-card"
    msys2_bin "ucrt64/bin/gpg-check-pattern.exe", "gpg-check-pattern"
    msys2_bin "ucrt64/bin/gpg-connect-agent.exe", "gpg-connect-agent"
    msys2_bin "ucrt64/bin/gpg-pair-tool.exe", "gpg-pair-tool"
    msys2_bin "ucrt64/bin/gpg-preset-passphrase.exe", "gpg-preset-passphrase"
    msys2_bin "ucrt64/bin/gpg-protect-tool.exe", "gpg-protect-tool"
    msys2_bin "ucrt64/bin/gpg-wks-client.exe", "gpg-wks-client"
    msys2_bin "ucrt64/bin/gpg.exe", "gpg"
    msys2_bin "ucrt64/bin/gpgconf-w32.exe", "gpgconf-w32"
    msys2_bin "ucrt64/bin/gpgconf.exe", "gpgconf"
    msys2_bin "ucrt64/bin/gpgscm.exe", "gpgscm"
    msys2_bin "ucrt64/bin/gpgsm.exe", "gpgsm"
    msys2_bin "ucrt64/bin/gpgtar.exe", "gpgtar"
    msys2_bin "ucrt64/bin/gpgv.exe", "gpgv"
    msys2_bin "ucrt64/bin/kbxutil.exe", "kbxutil"
    msys2_bin "ucrt64/bin/keyboxd.exe", "keyboxd"
    msys2_bin "ucrt64/bin/scdaemon.exe", "scdaemon"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dirmngr-client --version 2>&1")
  end
end
