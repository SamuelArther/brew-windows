class Coreutils < Formula
  desc "GNU File, Shell, and Text utilities"
  homepage "https://www.gnu.org/software/coreutils"
  url "https://repo.msys2.org/msys/x86_64/coreutils-8.32-5-x86_64.pkg.tar.zst"
  sha256 "62dfee1c39fd15f99c39802b35e82643bc14fffc16d6c76d4001caa385ec77e3"
  license "GPL-3.0-or-later"

  # Track the MSYS2 package this formula installs (upstream releases reach MSYS2 later).
  livecheck do
    url "https://repo.msys2.org/msys/x86_64/"
    regex(/href=.*?coreutils[._-]v?(\d+(?:\.\d+)+)-\d+-x86_64\.pkg\.t/i)
  end

  conflicts_with "idutils", because: "both install `gid` and `gid.1`"

  resource "bash" do
    url "https://repo.msys2.org/msys/x86_64/bash-5.3.20-2-x86_64.pkg.tar.zst"
    sha256 "0951e809c552dd7dd2835df1b2d590822187f14e717d809e942e4eb677d0adfd"
  end

  resource "db" do
    url "https://repo.msys2.org/msys/x86_64/db-6.2.32-6-x86_64.pkg.tar.zst"
    sha256 "41bc94acad3b53eaa92227ec34412305f648848ffbc963bcad88274517e13e8d"
  end

  resource "findutils" do
    url "https://repo.msys2.org/msys/x86_64/findutils-4.11.0-2-x86_64.pkg.tar.zst"
    sha256 "f19d3c6b1b073d4c5759ae83f5639d11a5f0227b772f7d86d600db573d9d5d32"
  end

  resource "gawk" do
    url "https://repo.msys2.org/msys/x86_64/gawk-5.4.1-1-x86_64.pkg.tar.zst"
    sha256 "4b1322cb6c83d61fde5100e229e3cabc5b7d01fc0d0d8bc311edc08df91916be"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "gdbm" do
    url "https://repo.msys2.org/msys/x86_64/gdbm-1.26-1-x86_64.pkg.tar.zst"
    sha256 "1f8e949be2bbe488c5644132db78ecda288c9945922e7ed2447384734e2adba2"
  end

  resource "gmp" do
    url "https://repo.msys2.org/msys/x86_64/gmp-6.3.0-2-x86_64.pkg.tar.zst"
    sha256 "f84536138e618d47a1df2da83916cfebbd449f159ffad52f994a71060a41cf34"
  end

  resource "grep" do
    url "https://repo.msys2.org/msys/x86_64/grep-1~3.0-7-x86_64.pkg.tar.zst"
    sha256 "2909b3bbef8d33d66980339fb7bd2d3d9145bae0f04191e6671df39868aea427"
  end

  resource "libdb" do
    url "https://repo.msys2.org/msys/x86_64/libdb-6.2.32-6-x86_64.pkg.tar.zst"
    sha256 "27a35786f3aab14e625c02df7bcd8a9dda0da782ce4f0549a427a21316b4d179"
  end

  resource "libgdbm" do
    url "https://repo.msys2.org/msys/x86_64/libgdbm-1.26-1-x86_64.pkg.tar.zst"
    sha256 "bb4b08b720dd3194db010589812e9c63bec3d29b32545e7cbd07b6443e51c5ed"
  end

  resource "libiconv" do
    url "https://repo.msys2.org/msys/x86_64/libiconv-1.19-1-x86_64.pkg.tar.zst"
    sha256 "0fa55ea2a6ccf97cf8c58b24b2615815e15e16e6e4e888091c263c2c83c5313d"
  end

  resource "libintl" do
    url "https://repo.msys2.org/msys/x86_64/libintl-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "336d66b9d95cf9c1804958f8e260762a3e83bf158ed5981f783bc772a31073cf"
  end

  resource "libpcre" do
    url "https://repo.msys2.org/msys/x86_64/libpcre-8.45-5-x86_64.pkg.tar.zst"
    sha256 "b4dc2eb7795cbacaa683101d720fa67f536262497a1d89934a58f5dab8d17d17"
  end

  resource "libreadline" do
    url "https://repo.msys2.org/msys/x86_64/libreadline-8.3.006-1-x86_64.pkg.tar.zst"
    sha256 "29e25c50bf6cd7a5e6f2253af76f54fe3c1a915313c7c18e98bedaa665c6adea"
  end

  resource "libxcrypt" do
    url "https://repo.msys2.org/msys/x86_64/libxcrypt-4.5.2-1-x86_64.pkg.tar.zst"
    sha256 "e63db0aa2b708359e25f61f506e2d2c92c315febf5f88cc9f386e4ec0bdf4fa5"
  end

  resource "m4" do
    url "https://repo.msys2.org/msys/x86_64/m4-1.4.21-1-x86_64.pkg.tar.zst"
    sha256 "b561e8871c7599502d9fae7196ee965b3fed3d7af09154eb8d2f5e4d9e5e5106"
  end

  resource "mpfr" do
    url "https://repo.msys2.org/msys/x86_64/mpfr-4.2.2-1-x86_64.pkg.tar.zst"
    sha256 "8f1b6c17c53e00494a488e03dfdf8fc742027ed03a5bd7b1b3a6a33f595a7498"
  end

  resource "msys2-runtime" do
    url "https://repo.msys2.org/msys/x86_64/msys2-runtime-3.6.11-3-x86_64.pkg.tar.zst"
    sha256 "b6478bd1b4cb4b2e07c45db725fee93f9424c19fa248240dcb7b7f4187cd92db"
  end

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  resource "perl" do
    url "https://repo.msys2.org/msys/x86_64/perl-5.42.3-3-x86_64.pkg.tar.zst"
    sha256 "6a4dbc38cdcc53d16239904b5c87d2277cffc49ed92c6a7796f8bf6b49517d5b"
  end

  resource "sed" do
    url "https://repo.msys2.org/msys/x86_64/sed-4.9-1-x86_64.pkg.tar.zst"
    sha256 "3748af28f69e946ec5a42e6670c9bbf6da7352dc93baaa537f69e99c5483b9fc"
  end

  # GNU coreutils via MSYS2; Windows filesystem and user semantics differ from Unix.

  def install
    msys2_install
    msys2_bin "usr/bin/base32.exe", "base32"
    msys2_bin "usr/bin/base64.exe", "base64"
    msys2_bin "usr/bin/basename.exe", "basename"
    msys2_bin "usr/bin/cat.exe", "cat"
    msys2_bin "usr/bin/chgrp.exe", "chgrp"
    msys2_bin "usr/bin/chmod.exe", "chmod"
    msys2_bin "usr/bin/chown.exe", "chown"
    msys2_bin "usr/bin/chroot.exe", "chroot"
    msys2_bin "usr/bin/cksum.exe", "cksum"
    msys2_bin "usr/bin/comm.exe", "comm"
    msys2_bin "usr/bin/cp.exe", "cp"
    msys2_bin "usr/bin/csplit.exe", "csplit"
    msys2_bin "usr/bin/cut.exe", "cut"
    msys2_bin "usr/bin/date.exe", "date"
    msys2_bin "usr/bin/dd.exe", "dd"
    msys2_bin "usr/bin/df.exe", "df"
    msys2_bin "usr/bin/dir.exe", "dir"
    msys2_bin "usr/bin/dircolors.exe", "dircolors"
    msys2_bin "usr/bin/dirname.exe", "dirname"
    msys2_bin "usr/bin/du.exe", "du"
    msys2_bin "usr/bin/echo.exe", "echo"
    msys2_bin "usr/bin/env.exe", "env"
    msys2_bin "usr/bin/expand.exe", "expand"
    msys2_bin "usr/bin/expr.exe", "expr"
    msys2_bin "usr/bin/factor.exe", "factor"
    msys2_bin "usr/bin/false.exe", "false"
    msys2_bin "usr/bin/fmt.exe", "fmt"
    msys2_bin "usr/bin/fold.exe", "fold"
    msys2_bin "usr/bin/head.exe", "head"
    msys2_bin "usr/bin/hostid.exe", "hostid"
    msys2_bin "usr/bin/id.exe", "id"
    msys2_bin "usr/bin/install.exe", "install"
    msys2_bin "usr/bin/join.exe", "join"
    msys2_bin "usr/bin/link.exe", "link"
    msys2_bin "usr/bin/ln.exe", "ln"
    msys2_bin "usr/bin/logname.exe", "logname"
    msys2_bin "usr/bin/ls.exe", "ls"
    msys2_bin "usr/bin/md5sum.exe", "md5sum"
    msys2_bin "usr/bin/mkdir.exe", "mkdir"
    msys2_bin "usr/bin/mkfifo.exe", "mkfifo"
    msys2_bin "usr/bin/mknod.exe", "mknod"
    msys2_bin "usr/bin/mktemp.exe", "mktemp"
    msys2_bin "usr/bin/mv.exe", "mv"
    msys2_bin "usr/bin/nice.exe", "nice"
    msys2_bin "usr/bin/nl.exe", "nl"
    msys2_bin "usr/bin/nohup.exe", "nohup"
    msys2_bin "usr/bin/nproc.exe", "nproc"
    msys2_bin "usr/bin/numfmt.exe", "numfmt"
    msys2_bin "usr/bin/od.exe", "od"
    msys2_bin "usr/bin/paste.exe", "paste"
    msys2_bin "usr/bin/pathchk.exe", "pathchk"
    msys2_bin "usr/bin/pinky.exe", "pinky"
    msys2_bin "usr/bin/pr.exe", "pr"
    msys2_bin "usr/bin/printenv.exe", "printenv"
    msys2_bin "usr/bin/printf.exe", "printf"
    msys2_bin "usr/bin/ptx.exe", "ptx"
    msys2_bin "usr/bin/pwd.exe", "pwd"
    msys2_bin "usr/bin/readlink.exe", "readlink"
    msys2_bin "usr/bin/realpath.exe", "realpath"
    msys2_bin "usr/bin/rm.exe", "rm"
    msys2_bin "usr/bin/rmdir.exe", "rmdir"
    msys2_bin "usr/bin/seq.exe", "seq"
    msys2_bin "usr/bin/sha1sum.exe", "sha1sum"
    msys2_bin "usr/bin/sha224sum.exe", "sha224sum"
    msys2_bin "usr/bin/sha256sum.exe", "sha256sum"
    msys2_bin "usr/bin/sha384sum.exe", "sha384sum"
    msys2_bin "usr/bin/sha512sum.exe", "sha512sum"
    msys2_bin "usr/bin/shred.exe", "shred"
    msys2_bin "usr/bin/shuf.exe", "shuf"
    msys2_bin "usr/bin/sleep.exe", "sleep"
    msys2_bin "usr/bin/sort.exe", "sort"
    msys2_bin "usr/bin/split.exe", "split"
    msys2_bin "usr/bin/stat.exe", "stat"
    msys2_bin "usr/bin/stdbuf.exe", "stdbuf"
    msys2_bin "usr/bin/stty.exe", "stty"
    msys2_bin "usr/bin/sum.exe", "sum"
    msys2_bin "usr/bin/sync.exe", "sync"
    msys2_bin "usr/bin/tac.exe", "tac"
    msys2_bin "usr/bin/tail.exe", "tail"
    msys2_bin "usr/bin/tee.exe", "tee"
    msys2_bin "usr/bin/test.exe", "test"
    msys2_bin "usr/bin/timeout.exe", "timeout"
    msys2_bin "usr/bin/touch.exe", "touch"
    msys2_bin "usr/bin/tr.exe", "tr"
    msys2_bin "usr/bin/true.exe", "true"
    msys2_bin "usr/bin/truncate.exe", "truncate"
    msys2_bin "usr/bin/tsort.exe", "tsort"
    msys2_bin "usr/bin/tty.exe", "tty"
    msys2_bin "usr/bin/uname.exe", "uname"
    msys2_bin "usr/bin/unexpand.exe", "unexpand"
    msys2_bin "usr/bin/uniq.exe", "uniq"
    msys2_bin "usr/bin/unlink.exe", "unlink"
    msys2_bin "usr/bin/users.exe", "users"
    msys2_bin "usr/bin/vdir.exe", "vdir"
    msys2_bin "usr/bin/wc.exe", "wc"
    msys2_bin "usr/bin/who.exe", "who"
    msys2_bin "usr/bin/whoami.exe", "whoami"
    msys2_bin "usr/bin/yes.exe", "yes"
    msys2_bin "usr/bin/[.exe", "["
    msys2_bin "usr/bin/arch.exe", "arch"
    msys2_bin "usr/bin/b2sum.exe", "b2sum"
    msys2_bin "usr/bin/basenc.exe", "basenc"
    msys2_bin "usr/bin/chcon.exe", "chcon"
    msys2_bin "usr/bin/gkill.exe", "gkill"
    msys2_bin "usr/bin/groups.exe", "groups"
    msys2_bin "usr/bin/hostname.exe", "hostname"
    msys2_bin "usr/bin/runcon.exe", "runcon"
  end

  test do
    (testpath/"test").write("test")
    (testpath/"test.sha1").write("a94a8fe5ccb19ba61c4c0873d391e987982fbbd3 test")
    system bin/"sha1sum", "-c", "test.sha1"
    system bin/"ln", "-f", "test", "test.sha1"
  end
end
