class UtilLinux < Formula
  desc "Collection of Linux utilities"
  homepage "https://github.com/util-linux/util-linux"
  url "https://repo.msys2.org/msys/x86_64/util-linux-2.40.2-2-x86_64.pkg.tar.zst"
  sha256 "969edb185d37986e6feddb3c54807598e3cc6774b736fec3a27d5d27731f7fdd"
  license all_of: [
    "BSD-3-Clause",
    "BSD-4-Clause-UC",
    "GPL-2.0-only",
    "GPL-2.0-or-later",
    "GPL-3.0-or-later",
    "LGPL-2.1-or-later",
    :public_domain,
  ]

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url :homepage
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  resource "coreutils" do
    url "https://repo.msys2.org/msys/x86_64/coreutils-8.32-5-x86_64.pkg.tar.zst"
    sha256 "62dfee1c39fd15f99c39802b35e82643bc14fffc16d6c76d4001caa385ec77e3"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "gmp" do
    url "https://repo.msys2.org/msys/x86_64/gmp-6.3.0-2-x86_64.pkg.tar.zst"
    sha256 "f84536138e618d47a1df2da83916cfebbd449f159ffad52f994a71060a41cf34"
  end

  resource "libiconv" do
    url "https://repo.msys2.org/msys/x86_64/libiconv-1.19-1-x86_64.pkg.tar.zst"
    sha256 "0fa55ea2a6ccf97cf8c58b24b2615815e15e16e6e4e888091c263c2c83c5313d"
  end

  resource "libintl" do
    url "https://repo.msys2.org/msys/x86_64/libintl-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "336d66b9d95cf9c1804958f8e260762a3e83bf158ed5981f783bc772a31073cf"
  end

  resource "libreadline" do
    url "https://repo.msys2.org/msys/x86_64/libreadline-8.3.006-1-x86_64.pkg.tar.zst"
    sha256 "29e25c50bf6cd7a5e6f2253af76f54fe3c1a915313c7c18e98bedaa665c6adea"
  end

  resource "libsqlite" do
    url "https://repo.msys2.org/msys/x86_64/libsqlite-3.54.0-1-x86_64.pkg.tar.zst"
    sha256 "089e17fe8647da22b790dc2d5629d56cf53b5da5685d6555cb172400f32fe70f"
  end

  resource "libutil-linux" do
    url "https://repo.msys2.org/msys/x86_64/libutil-linux-2.40.2-2-x86_64.pkg.tar.zst"
    sha256 "148ec8e668cb0a30ea0c4e2687e16e0953d6628305598f4c3225bedf2564192d"
  end

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/agetty.exe", "agetty"
    msys2_bin "usr/bin/blkid.exe", "blkid"
    msys2_bin "usr/bin/cal.exe", "cal"
    msys2_bin "usr/bin/cfdisk.exe", "cfdisk"
    msys2_bin "usr/bin/chrt.exe", "chrt"
    msys2_bin "usr/bin/col.exe", "col"
    msys2_bin "usr/bin/colcrt.exe", "colcrt"
    msys2_bin "usr/bin/colrm.exe", "colrm"
    msys2_bin "usr/bin/column.exe", "column"
    msys2_bin "usr/bin/fallocate.exe", "fallocate"
    msys2_bin "usr/bin/fdisk.exe", "fdisk"
    msys2_bin "usr/bin/findfs.exe", "findfs"
    msys2_bin "usr/bin/flock.exe", "flock"
    msys2_bin "usr/bin/fsck.cramfs.exe", "fsck.cramfs"
    msys2_bin "usr/bin/fsck.minix.exe", "fsck.minix"
    msys2_bin "usr/bin/getopt.exe", "getopt"
    msys2_bin "usr/bin/hardlink.exe", "hardlink"
    msys2_bin "usr/bin/hexdump.exe", "hexdump"
    msys2_bin "usr/bin/ipcmk.exe", "ipcmk"
    msys2_bin "usr/bin/isosize.exe", "isosize"
    msys2_bin "usr/bin/lastlog2.exe", "lastlog2"
    msys2_bin "usr/bin/logger.exe", "logger"
    msys2_bin "usr/bin/look.exe", "look"
    msys2_bin "usr/bin/mcookie.exe", "mcookie"
    msys2_bin "usr/bin/mkfs.bfs.exe", "mkfs.bfs"
    msys2_bin "usr/bin/mkfs.cramfs.exe", "mkfs.cramfs"
    msys2_bin "usr/bin/mkfs.exe", "mkfs"
    msys2_bin "usr/bin/mkfs.minix.exe", "mkfs.minix"
    msys2_bin "usr/bin/mkswap.exe", "mkswap"
    msys2_bin "usr/bin/more.exe", "more"
    msys2_bin "usr/bin/namei.exe", "namei"
    msys2_bin "usr/bin/nologin.exe", "nologin"
    msys2_bin "usr/bin/rename.exe", "rename"
    msys2_bin "usr/bin/renice.exe", "renice"
    msys2_bin "usr/bin/rev.exe", "rev"
    msys2_bin "usr/bin/script.exe", "script"
    msys2_bin "usr/bin/scriptlive.exe", "scriptlive"
    msys2_bin "usr/bin/scriptreplay.exe", "scriptreplay"
    msys2_bin "usr/bin/setpgid.exe", "setpgid"
    msys2_bin "usr/bin/setsid.exe", "setsid"
    msys2_bin "usr/bin/sfdisk.exe", "sfdisk"
    msys2_bin "usr/bin/swaplabel.exe", "swaplabel"
    msys2_bin "usr/bin/taskset.exe", "taskset"
    msys2_bin "usr/bin/ul.exe", "ul"
    msys2_bin "usr/bin/uuidd.exe", "uuidd"
    msys2_bin "usr/bin/uuidgen.exe", "uuidgen"
    msys2_bin "usr/bin/uuidparse.exe", "uuidparse"
    msys2_bin "usr/bin/whereis.exe", "whereis"
    msys2_bin "usr/bin/wipefs.exe", "wipefs"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/agetty --version 2>&1")
  end
end
