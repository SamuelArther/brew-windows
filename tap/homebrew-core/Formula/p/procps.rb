class Procps < Formula
  desc "Utilities for browsing procfs"
  homepage "https://gitlab.com/procps-ng/procps"
  url "https://repo.msys2.org/msys/x86_64/procps-ng-4.0.5-1-x86_64.pkg.tar.zst"
  sha256 "ce092fd0a2cc952a29f80b6eee207e6391891f4eefbfd737cc1b912b5bc49fe3"
  license "GPL-2.0-or-later"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://gitlab.com/procps-ng/procps/-/archive/v4.0.7/procps-v4.0.7.tar.gz"
  end

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

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/free.exe", "free"
    msys2_bin "usr/bin/pgrep.exe", "pgrep"
    msys2_bin "usr/bin/pidof.exe", "pidof"
    msys2_bin "usr/bin/pkill.exe", "pkill"
    msys2_bin "usr/bin/pmap.exe", "pmap"
    msys2_bin "usr/bin/procps.exe", "procps"
    msys2_bin "usr/bin/top.exe", "top"
    msys2_bin "usr/bin/uptime.exe", "uptime"
    msys2_bin "usr/bin/vmstat.exe", "vmstat"
    msys2_bin "usr/bin/w.exe", "w"
    msys2_bin "usr/bin/watch.exe", "watch"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/procps --version 2>&1")
  end
end
