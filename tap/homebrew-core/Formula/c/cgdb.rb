class Cgdb < Formula
  desc "Curses-based interface to the GNU Debugger"
  homepage "https://cgdb.github.io/"
  url "https://repo.msys2.org/msys/x86_64/cgdb-0.8.0-2-x86_64.pkg.tar.zst"
  sha256 "6837fa3634ffd43d01f5ccb4d5b5c3fb68aa0e25b2e553afd78997f826e63f3f"
  license "GPL-2.0-or-later"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://cgdb.me/files/"
    regex(/href=.*?cgdb[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  resource "binutils" do
    url "https://repo.msys2.org/msys/x86_64/binutils-2.47-1-x86_64.pkg.tar.zst"
    sha256 "b7ddaca6e8b63774cdb1f5ac2720963cd76f8f62ca7e2667b1b0f008ed7ecba2"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "gdb" do
    url "https://repo.msys2.org/msys/x86_64/gdb-18.1-1-x86_64.pkg.tar.zst"
    sha256 "1c7f2184620aa38dd408b15bcec55c5878509c84b0e7c6c0be1362e45d43f869"
  end

  resource "gmp" do
    url "https://repo.msys2.org/msys/x86_64/gmp-6.3.0-2-x86_64.pkg.tar.zst"
    sha256 "f84536138e618d47a1df2da83916cfebbd449f159ffad52f994a71060a41cf34"
  end

  resource "iconv" do
    url "https://repo.msys2.org/msys/x86_64/iconv-1.19-1-x86_64.pkg.tar.zst"
    sha256 "f1916b4fa349612c7edde73c2f8f50b1371d5945e7503b29169f08310d033633"
  end

  resource "libbz2" do
    url "https://repo.msys2.org/msys/x86_64/libbz2-1.0.8-4-x86_64.pkg.tar.zst"
    sha256 "89fb0e1478b22b80effda55ab4ae7549388d01245d3841a096d8a2d63236c2a1"
  end

  resource "libexpat" do
    url "https://repo.msys2.org/msys/x86_64/libexpat-2.9.0-1-x86_64.pkg.tar.zst"
    sha256 "3021e1119cbe6a384977914c1dc099a98ce861505cc07229a842e8e1253597e5"
  end

  resource "libffi" do
    url "https://repo.msys2.org/msys/x86_64/libffi-3.8.0-1-x86_64.pkg.tar.zst"
    sha256 "e0a21ae5ee3f0ce1ff3f446ea2c95ad462803081014aecfa8fc1cd22bc4df187"
  end

  resource "libgc" do
    url "https://repo.msys2.org/msys/x86_64/libgc-8.2.12-1-x86_64.pkg.tar.zst"
    sha256 "a6bd62a8473c57d3233ab5d4baaf090259fc3a44032d7f1938600c08d2cc2f88"
  end

  resource "libguile" do
    url "https://repo.msys2.org/msys/x86_64/libguile-3.0.11-1-x86_64.pkg.tar.zst"
    sha256 "455c95505ac126fe1d15edb3983c2ffcecbaa0f5b1775dca1d022190b776edc2"
  end

  resource "libiconv" do
    url "https://repo.msys2.org/msys/x86_64/libiconv-1.19-1-x86_64.pkg.tar.zst"
    sha256 "0fa55ea2a6ccf97cf8c58b24b2615815e15e16e6e4e888091c263c2c83c5313d"
  end

  resource "libintl" do
    url "https://repo.msys2.org/msys/x86_64/libintl-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "336d66b9d95cf9c1804958f8e260762a3e83bf158ed5981f783bc772a31073cf"
  end

  resource "liblzma" do
    url "https://repo.msys2.org/msys/x86_64/liblzma-5.8.4-1-x86_64.pkg.tar.zst"
    sha256 "4b7d61656e72a5651a797680b66186338dce4f8fbc62a637ab3b59922dd01074"
  end

  resource "libopenssl" do
    url "https://repo.msys2.org/msys/x86_64/libopenssl-3.6.5-1-x86_64.pkg.tar.zst"
    sha256 "05f6cc946f0dde3aecc3632007fa1d874e40720226117b750327bc2a01846325"
  end

  resource "libreadline" do
    url "https://repo.msys2.org/msys/x86_64/libreadline-8.3.006-1-x86_64.pkg.tar.zst"
    sha256 "29e25c50bf6cd7a5e6f2253af76f54fe3c1a915313c7c18e98bedaa665c6adea"
  end

  resource "libsqlite" do
    url "https://repo.msys2.org/msys/x86_64/libsqlite-3.54.0-1-x86_64.pkg.tar.zst"
    sha256 "089e17fe8647da22b790dc2d5629d56cf53b5da5685d6555cb172400f32fe70f"
  end

  resource "libunistring" do
    url "https://repo.msys2.org/msys/x86_64/libunistring-1.4.2-1-x86_64.pkg.tar.zst"
    sha256 "bcd04706d95b5e7127d39834565bceed6b400192f5fb51599745aba28d5da018"
  end

  resource "libxcrypt" do
    url "https://repo.msys2.org/msys/x86_64/libxcrypt-4.5.2-1-x86_64.pkg.tar.zst"
    sha256 "e63db0aa2b708359e25f61f506e2d2c92c315febf5f88cc9f386e4ec0bdf4fa5"
  end

  resource "libxxhash" do
    url "https://repo.msys2.org/msys/x86_64/libxxhash-0.8.4-1-x86_64.pkg.tar.zst"
    sha256 "ba784ad117626401cd5d3ec0cf70399f9de72a63755f96c28fa2032303f9c330"
  end

  resource "libzstd" do
    url "https://repo.msys2.org/msys/x86_64/libzstd-1.5.7-1-x86_64.pkg.tar.zst"
    sha256 "5e370b2e725a0a7e5ff58d84653c9a0cd5a10b9cfe930f2d949a28ad2a11c1f2"
  end

  resource "mpc" do
    url "https://repo.msys2.org/msys/x86_64/mpc-1.4.1-1-x86_64.pkg.tar.zst"
    sha256 "0f5073ec2e8be265854ee3c7cb1079b5e8e02264d53e659d8414988c6c182f16"
  end

  resource "mpdecimal" do
    url "https://repo.msys2.org/msys/x86_64/mpdecimal-4.0.1-1-x86_64.pkg.tar.zst"
    sha256 "01ae3016641af4d499329c68e00a34664b7c7d9095dc6a859317441507132d41"
  end

  resource "mpfr" do
    url "https://repo.msys2.org/msys/x86_64/mpfr-4.2.2-1-x86_64.pkg.tar.zst"
    sha256 "8f1b6c17c53e00494a488e03dfdf8fc742027ed03a5bd7b1b3a6a33f595a7498"
  end

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  resource "python" do
    url "https://repo.msys2.org/msys/x86_64/python-3.12.15-1-x86_64.pkg.tar.zst"
    sha256 "5672cd7d84b1415adce1cfc1555e2fc12e489c8cb0c0f787cf2ed6f1ecdf73f8"
  end

  resource "xxhash" do
    url "https://repo.msys2.org/msys/x86_64/xxhash-0.8.4-1-x86_64.pkg.tar.zst"
    sha256 "07148c285ee32e86daf67d1e54cd7b5a928360cbe963c33ad7d6be570adab806"
  end

  resource "zlib" do
    url "https://repo.msys2.org/msys/x86_64/zlib-1.3.2-1-x86_64.pkg.tar.zst"
    sha256 "a04aa79996c57f0db936be66cf94326d7e67e9cd8dbffe4cf6e97693d0a1d9ef"
  end

  resource "zstd" do
    url "https://repo.msys2.org/msys/x86_64/zstd-1.5.7-1-x86_64.pkg.tar.zst"
    sha256 "7b8ab77828c5a1efe53ebc53e3ea9d719bfbd609bf4c810eaa88d626188ed407"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/cgdb.exe", "cgdb"
  end

  test do
    system bin/"cgdb", "--version"
  end
end
