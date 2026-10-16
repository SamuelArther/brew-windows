class Idutils < Formula
  desc "ID database and query tools"
  homepage "https://www.gnu.org/software/idutils/"
  url "https://repo.msys2.org/msys/x86_64/idutils-4.6-3-x86_64.pkg.tar.zst"
  sha256 "71b5bce5ece22e20ec34b7e0d44806f5aab9569f3dc8e87e46086a7cd93d5bf5"
  license "GPL-3.0-or-later"
  revision 1

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://ftpmirror.gnu.org/idutils/idutils-4.6.tar.xz"
    regex(/href=.*?idutils[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  conflicts_with "coreutils", because: "both install `gid` and `gid.1`"

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "libiconv" do
    url "https://repo.msys2.org/msys/x86_64/libiconv-1.19-1-x86_64.pkg.tar.zst"
    sha256 "0fa55ea2a6ccf97cf8c58b24b2615815e15e16e6e4e888091c263c2c83c5313d"
  end

  resource "libintl" do
    url "https://repo.msys2.org/msys/x86_64/libintl-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "336d66b9d95cf9c1804958f8e260762a3e83bf158ed5981f783bc772a31073cf"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/aid.exe", "aid"
    msys2_bin "usr/bin/eid.exe", "eid"
    msys2_bin "usr/bin/fid.exe", "fid"
    msys2_bin "usr/bin/fnid.exe", "fnid"
    msys2_bin "usr/bin/gid.exe", "gid"
    msys2_bin "usr/bin/lid.exe", "lid"
    msys2_bin "usr/bin/mkid.exe", "mkid"
    msys2_bin "usr/bin/xtokid.exe", "xtokid"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/aid --version 2>&1")
  end
end
