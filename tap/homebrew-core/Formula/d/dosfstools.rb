class Dosfstools < Formula
  desc "Tools to create, check and label file systems of the FAT family"
  homepage "https://github.com/dosfstools"
  url "https://repo.msys2.org/msys/x86_64/dosfstools-4.2-1-x86_64.pkg.tar.zst"
  sha256 "a37cc088eb77356a53a505a4175f8d301bacaf803808518f2cc88cef0f2ef906"
  license "GPL-3.0-or-later"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://github.com/dosfstools/dosfstools/releases/download/v4.2/dosfstools-4.2.tar.gz"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "iconv" do
    url "https://repo.msys2.org/msys/x86_64/iconv-1.19-1-x86_64.pkg.tar.zst"
    sha256 "f1916b4fa349612c7edde73c2f8f50b1371d5945e7503b29169f08310d033633"
  end

  resource "libiconv" do
    url "https://repo.msys2.org/msys/x86_64/libiconv-1.19-1-x86_64.pkg.tar.zst"
    sha256 "0fa55ea2a6ccf97cf8c58b24b2615815e15e16e6e4e888091c263c2c83c5313d"
  end

  resource "libiconv-devel" do
    url "https://repo.msys2.org/msys/x86_64/libiconv-devel-1.19-1-x86_64.pkg.tar.zst"
    sha256 "b4836f02199a2ae02d5a836d5a6dc4a174a055da79c30e256635e638b52e2c5d"
  end

  resource "libintl" do
    url "https://repo.msys2.org/msys/x86_64/libintl-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "336d66b9d95cf9c1804958f8e260762a3e83bf158ed5981f783bc772a31073cf"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/dosfsck.exe", "dosfsck"
    msys2_bin "usr/bin/dosfslabel.exe", "dosfslabel"
    msys2_bin "usr/bin/fatlabel.exe", "fatlabel"
    msys2_bin "usr/bin/fsck.fat.exe", "fsck.fat"
    msys2_bin "usr/bin/fsck.msdos.exe", "fsck.msdos"
    msys2_bin "usr/bin/fsck.vfat.exe", "fsck.vfat"
    msys2_bin "usr/bin/mkdosfs.exe", "mkdosfs"
    msys2_bin "usr/bin/mkfs.fat.exe", "mkfs.fat"
    msys2_bin "usr/bin/mkfs.msdos.exe", "mkfs.msdos"
    msys2_bin "usr/bin/mkfs.vfat.exe", "mkfs.vfat"
  end

  test do
    system bin/"dosfsck", "--help"
  end
end
