class Breezy < Formula
  desc "Version control system implemented in Python with multi-format support"
  homepage "https://www.breezy-vcs.org/"
  url "https://repo.msys2.org/msys/x86_64/breezy-3.3.22-1-x86_64.pkg.tar.zst"
  sha256 "f40128f0e53a582a3dde9f34f66bf69bb2bc080a478c571123a43f0ada813da5"
  license "GPL-2.0-or-later"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://files.pythonhosted.org/packages/59/1d/68956e351c1af8116ec8525c85faf82ed2484f38f6f4fed4755759073500/breezy-3.3.21.tar.gz"
  end

  resource "binutils" do
    url "https://repo.msys2.org/msys/x86_64/binutils-2.47-1-x86_64.pkg.tar.zst"
    sha256 "b7ddaca6e8b63774cdb1f5ac2720963cd76f8f62ca7e2667b1b0f008ed7ecba2"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
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

  resource "libxcrypt" do
    url "https://repo.msys2.org/msys/x86_64/libxcrypt-4.5.2-1-x86_64.pkg.tar.zst"
    sha256 "e63db0aa2b708359e25f61f506e2d2c92c315febf5f88cc9f386e4ec0bdf4fa5"
  end

  resource "libzstd" do
    url "https://repo.msys2.org/msys/x86_64/libzstd-1.5.7-1-x86_64.pkg.tar.zst"
    sha256 "5e370b2e725a0a7e5ff58d84653c9a0cd5a10b9cfe930f2d949a28ad2a11c1f2"
  end

  resource "mpdecimal" do
    url "https://repo.msys2.org/msys/x86_64/mpdecimal-4.0.1-1-x86_64.pkg.tar.zst"
    sha256 "01ae3016641af4d499329c68e00a34664b7c7d9095dc6a859317441507132d41"
  end

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  resource "python" do
    url "https://repo.msys2.org/msys/x86_64/python-3.12.15-1-x86_64.pkg.tar.zst"
    sha256 "5672cd7d84b1415adce1cfc1555e2fc12e489c8cb0c0f787cf2ed6f1ecdf73f8"
  end

  resource "python-configobj" do
    url "https://repo.msys2.org/msys/x86_64/python-configobj-5.0.9-2-any.pkg.tar.zst"
    sha256 "37a032e517f6482fd10a6ec9cde447de9de49395884659169bd6a1a08cab4a57"
  end

  resource "python-dulwich" do
    url "https://repo.msys2.org/msys/x86_64/python-dulwich-1.2.17-1-any.pkg.tar.zst"
    sha256 "eedae7cd804440cbf6568231ef80bafb24901ac672a6f493b0ee7fc4a539fb3d"
  end

  resource "python-fastbencode" do
    url "https://repo.msys2.org/msys/x86_64/python-fastbencode-0.3.11-1-x86_64.pkg.tar.zst"
    sha256 "5f9e2ed3ea736c3a8d547a8ecfabc84613b8891f7f6ec3058e00fe130b05e2e3"
  end

  resource "python-fastimport" do
    url "https://repo.msys2.org/msys/x86_64/python-fastimport-0.9.16-1-any.pkg.tar.zst"
    sha256 "a79761aa273e04e931952028c07628d50a9c73612e06b50ac2f4963b564315a0"
  end

  resource "python-merge3" do
    url "https://repo.msys2.org/msys/x86_64/python-merge3-0.0.16-1-any.pkg.tar.zst"
    sha256 "4498eebc1addc1b1691fbe008543e8d969aa9c44f008992637a62abd9ec3c47c"
  end

  resource "python-patiencediff" do
    url "https://repo.msys2.org/msys/x86_64/python-patiencediff-1.0.1-1-x86_64.pkg.tar.zst"
    sha256 "a10efc707c24373090fd77d9e150af9d3569bd5e0cc9960bd82a0fd196884fc7"
  end

  resource "python-urllib3" do
    url "https://repo.msys2.org/msys/x86_64/python-urllib3-2.8.0-1-any.pkg.tar.zst"
    sha256 "b8fbb7bc0b3886dde91d8690728e479491b17409add4e46ee20e4580ec20c35c"
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
    msys2_bin "usr/bin/brz.exe", "brz"
    msys2_bin "usr/bin/bzr.exe", "bzr"
  end

  test do
    assert_path_exists bin/"brz.exe"
  end
end
