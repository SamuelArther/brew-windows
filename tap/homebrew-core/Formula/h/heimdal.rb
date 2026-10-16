class Heimdal < Formula
  desc "Free Kerberos 5 implementation"
  homepage "https://github.com/heimdal/heimdal"
  url "https://repo.msys2.org/msys/x86_64/heimdal-7.8.0-7-x86_64.pkg.tar.zst"
  sha256 "c1e9dc49b6eaffd145abd73c8b516acca89eac64fa90031e116ac93c3b493a1e"
  license all_of: [
    "BSD-3-Clause",
    "BSD-2-Clause",    # lib/gssapi/mech/
    "HPND-export2-US", # kdc/announce.c
    :public_domain,    # lib/hcrypto/libtommath/
  ]
  revision 2

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://github.com/heimdal/heimdal/releases/download/heimdal-7.8.0/heimdal-7.8.0.tar.gz"
    regex(/heimdal[._-]v?(\d+(?:\.\d+)+)/i)
    strategy :github_latest
  end

  keg_only "it conflicts with Kerberos"

  resource "bash" do
    url "https://repo.msys2.org/msys/x86_64/bash-5.3.20-2-x86_64.pkg.tar.zst"
    sha256 "0951e809c552dd7dd2835df1b2d590822187f14e717d809e942e4eb677d0adfd"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "heimdal-libs" do
    url "https://repo.msys2.org/msys/x86_64/heimdal-libs-7.8.0-7-x86_64.pkg.tar.zst"
    sha256 "badbf9d464fe8498668451b8d53cf84c7ec3bd0e7bcc0ea158bf6339a30a2416"
  end

  resource "libdb" do
    url "https://repo.msys2.org/msys/x86_64/libdb-6.2.32-6-x86_64.pkg.tar.zst"
    sha256 "27a35786f3aab14e625c02df7bcd8a9dda0da782ce4f0549a427a21316b4d179"
  end

  resource "libedit" do
    url "https://repo.msys2.org/msys/x86_64/libedit-20240808_3.1-1-x86_64.pkg.tar.zst"
    sha256 "aada8ba7dfe41fdcac28d3706c0dc7c621b7667035a5e629cb5f3af7fb3f7de0"
  end

  resource "libopenssl" do
    url "https://repo.msys2.org/msys/x86_64/libopenssl-3.6.5-1-x86_64.pkg.tar.zst"
    sha256 "05f6cc946f0dde3aecc3632007fa1d874e40720226117b750327bc2a01846325"
  end

  resource "libsqlite" do
    url "https://repo.msys2.org/msys/x86_64/libsqlite-3.54.0-1-x86_64.pkg.tar.zst"
    sha256 "089e17fe8647da22b790dc2d5629d56cf53b5da5685d6555cb172400f32fe70f"
  end

  resource "libxcrypt" do
    url "https://repo.msys2.org/msys/x86_64/libxcrypt-4.5.2-1-x86_64.pkg.tar.zst"
    sha256 "e63db0aa2b708359e25f61f506e2d2c92c315febf5f88cc9f386e4ec0bdf4fa5"
  end

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/afslog.exe", "afslog"
    msys2_bin "usr/bin/bsearch.exe", "bsearch"
    msys2_bin "usr/bin/gsstool.exe", "gsstool"
    msys2_bin "usr/bin/heimtools.exe", "heimtools"
    msys2_bin "usr/bin/hxtool.exe", "hxtool"
    msys2_bin "usr/bin/idn-lookup.exe", "idn-lookup"
    msys2_bin "usr/bin/iprop-log.exe", "iprop-log"
    msys2_bin "usr/bin/kadmin.exe", "kadmin"
    msys2_bin "usr/bin/kdestroy.exe", "kdestroy"
    msys2_bin "usr/bin/kf.exe", "kf"
    msys2_bin "usr/bin/kgetcred.exe", "kgetcred"
    msys2_bin "usr/bin/kinit.exe", "kinit"
    msys2_bin "usr/bin/klist.exe", "klist"
    msys2_bin "usr/bin/kpasswd.exe", "kpasswd"
    msys2_bin "usr/bin/kstash.exe", "kstash"
    msys2_bin "usr/bin/ksu.exe", "ksu"
    msys2_bin "usr/bin/kswitch.exe", "kswitch"
    msys2_bin "usr/bin/ktutil.exe", "ktutil"
    msys2_bin "usr/bin/otp.exe", "otp"
    msys2_bin "usr/bin/otpprint.exe", "otpprint"
    msys2_bin "usr/bin/pagsh.exe", "pagsh"
    msys2_bin "usr/bin/string2key.exe", "string2key"
    msys2_bin "usr/bin/verify_krb5_conf.exe", "verify_krb5_conf"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/afslog --version 2>&1")
  end
end
