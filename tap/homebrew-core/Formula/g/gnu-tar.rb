class GnuTar < Formula
  desc "GNU version of the tar archiving utility"
  homepage "https://www.gnu.org/software/tar/"
  url "https://repo.msys2.org/msys/x86_64/tar-1.35-3-x86_64.pkg.tar.zst"
  sha256 "71983bed200c4026cdec157837d989728d3de65e8cf644e03876df41a0f1b7c4"
  license "GPL-3.0-or-later"

  # GNU tar via MSYS2, exposed as tar and gtar (Windows ships bsdtar as tar.exe).
  # gzip, bzip2, xz and zstd are bundled for -z/-j/-J/--zstd.

  resource "bash" do
    url "https://repo.msys2.org/msys/x86_64/bash-5.3.20-2-x86_64.pkg.tar.zst"
    sha256 "0951e809c552dd7dd2835df1b2d590822187f14e717d809e942e4eb677d0adfd"
  end

  resource "bzip2" do
    url "https://repo.msys2.org/msys/x86_64/bzip2-1.0.8-4-x86_64.pkg.tar.zst"
    sha256 "635c483775de60663bd38859bb4624e6aaa1086354acf8e36513b75c24f64b3c"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "gzip" do
    url "https://repo.msys2.org/msys/x86_64/gzip-1.15-1-x86_64.pkg.tar.zst"
    sha256 "3f967c87cf041391d0d43819c91174742917ccc2d1bb9419050df29ef22f4bfd"
  end

  resource "less" do
    url "https://repo.msys2.org/msys/x86_64/less-710-1-x86_64.pkg.tar.zst"
    sha256 "894a643ecd01861490e5767873d67b55cd5a2faf2c77b0b24a62dbba7bd038fd"
  end

  resource "libbz2" do
    url "https://repo.msys2.org/msys/x86_64/libbz2-1.0.8-4-x86_64.pkg.tar.zst"
    sha256 "89fb0e1478b22b80effda55ab4ae7549388d01245d3841a096d8a2d63236c2a1"
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

  resource "libpcre2_8" do
    url "https://repo.msys2.org/msys/x86_64/libpcre2_8-10.49-1-x86_64.pkg.tar.zst"
    sha256 "2de42bf4656e3952a72809663e85899be76c7020e016687055ac0acd05145a3b"
  end

  resource "libzstd" do
    url "https://repo.msys2.org/msys/x86_64/libzstd-1.5.7-1-x86_64.pkg.tar.zst"
    sha256 "5e370b2e725a0a7e5ff58d84653c9a0cd5a10b9cfe930f2d949a28ad2a11c1f2"
  end

  resource "msys2-runtime" do
    url "https://repo.msys2.org/msys/x86_64/msys2-runtime-3.6.11-3-x86_64.pkg.tar.zst"
    sha256 "b6478bd1b4cb4b2e07c45db725fee93f9424c19fa248240dcb7b7f4187cd92db"
  end

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  resource "xz" do
    url "https://repo.msys2.org/msys/x86_64/xz-5.8.4-1-x86_64.pkg.tar.zst"
    sha256 "cad72459be8f5b99920bed2f122dfe81e05fc599c33f5b871bd35d72b726ef9e"
  end

  resource "zstd" do
    url "https://repo.msys2.org/msys/x86_64/zstd-1.5.7-1-x86_64.pkg.tar.zst"
    sha256 "7b8ab77828c5a1efe53ebc53e3ea9d719bfbd609bf4c810eaa88d626188ed407"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/tar.exe", "tar"
    msys2_bin "usr/bin/tar.exe", "gtar"
  end

  test do
    (testpath/"test").write("test")
    system bin/"gtar", "-czvf", "test.tar.gz", "test"
    assert_match "test", shell_output("#{bin}/gtar -xOzf test.tar.gz")
  end
end
