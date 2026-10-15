class Subversion < Formula
  desc "Version control system designed to be a better CVS"
  homepage "https://subversion.apache.org/"
  url "https://repo.msys2.org/msys/x86_64/subversion-1.14.5-3-x86_64.pkg.tar.zst"
  sha256 "8f6878df278efe67d2aeaec47e9bff65d1da54a6a0f8806c01848d5ddc435d0c"
  license "Apache-2.0"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://www.apache.org/dyn/closer.lua?path=subversion/subversion-1.15.0.tar.bz2"
  end

  resource "apr" do
    url "https://repo.msys2.org/msys/x86_64/apr-1.7.6-2-x86_64.pkg.tar.zst"
    sha256 "5494c349d7f60adae413f0407218a61f2d684021f6b7b406715ba72cc553904a"
  end

  resource "apr-util" do
    url "https://repo.msys2.org/msys/x86_64/apr-util-1.6.5-1-x86_64.pkg.tar.zst"
    sha256 "392ad8b968f97e64a541999b6560ede7288e1dc2ebdb1e507efb41a266a9e838"
  end

  resource "bash" do
    url "https://repo.msys2.org/msys/x86_64/bash-5.3.20-2-x86_64.pkg.tar.zst"
    sha256 "0951e809c552dd7dd2835df1b2d590822187f14e717d809e942e4eb677d0adfd"
  end

  resource "expat" do
    url "https://repo.msys2.org/msys/x86_64/expat-2.9.0-1-x86_64.pkg.tar.zst"
    sha256 "20374d2f31907c90a238d19b108e3442a06913a5f20214492a673bdd89ac15d1"
  end

  resource "file" do
    url "https://repo.msys2.org/msys/x86_64/file-5.48-1-x86_64.pkg.tar.zst"
    sha256 "f55026638fe4ed4b31ee3cf316843ac190d9332816bff8dcb0a99fd8c6350608"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "heimdal-libs" do
    url "https://repo.msys2.org/msys/x86_64/heimdal-libs-7.8.0-7-x86_64.pkg.tar.zst"
    sha256 "badbf9d464fe8498668451b8d53cf84c7ec3bd0e7bcc0ea158bf6339a30a2416"
  end

  resource "libbz2" do
    url "https://repo.msys2.org/msys/x86_64/libbz2-1.0.8-4-x86_64.pkg.tar.zst"
    sha256 "89fb0e1478b22b80effda55ab4ae7549388d01245d3841a096d8a2d63236c2a1"
  end

  resource "libdb" do
    url "https://repo.msys2.org/msys/x86_64/libdb-6.2.32-6-x86_64.pkg.tar.zst"
    sha256 "27a35786f3aab14e625c02df7bcd8a9dda0da782ce4f0549a427a21316b4d179"
  end

  resource "libedit" do
    url "https://repo.msys2.org/msys/x86_64/libedit-20240808_3.1-1-x86_64.pkg.tar.zst"
    sha256 "aada8ba7dfe41fdcac28d3706c0dc7c621b7667035a5e629cb5f3af7fb3f7de0"
  end

  resource "libexpat" do
    url "https://repo.msys2.org/msys/x86_64/libexpat-2.9.0-1-x86_64.pkg.tar.zst"
    sha256 "3021e1119cbe6a384977914c1dc099a98ce861505cc07229a842e8e1253597e5"
  end

  resource "libiconv" do
    url "https://repo.msys2.org/msys/x86_64/libiconv-1.19-1-x86_64.pkg.tar.zst"
    sha256 "0fa55ea2a6ccf97cf8c58b24b2615815e15e16e6e4e888091c263c2c83c5313d"
  end

  resource "libintl" do
    url "https://repo.msys2.org/msys/x86_64/libintl-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "336d66b9d95cf9c1804958f8e260762a3e83bf158ed5981f783bc772a31073cf"
  end

  resource "liblz4" do
    url "https://repo.msys2.org/msys/x86_64/liblz4-1.10.0-1-x86_64.pkg.tar.zst"
    sha256 "1cbbe51ee91bf32691aabb15bdb7c8548e8b5ef8195122ef31f10edcca6e952f"
  end

  resource "liblzma" do
    url "https://repo.msys2.org/msys/x86_64/liblzma-5.8.4-1-x86_64.pkg.tar.zst"
    sha256 "4b7d61656e72a5651a797680b66186338dce4f8fbc62a637ab3b59922dd01074"
  end

  resource "libopenssl" do
    url "https://repo.msys2.org/msys/x86_64/libopenssl-3.6.5-1-x86_64.pkg.tar.zst"
    sha256 "05f6cc946f0dde3aecc3632007fa1d874e40720226117b750327bc2a01846325"
  end

  resource "libsasl" do
    url "https://repo.msys2.org/msys/x86_64/libsasl-2.1.28-5-x86_64.pkg.tar.zst"
    sha256 "c93c8c7279fcfc76c7d5339bb5bb4659e36114e45fd174d81838e41a1f4c6e1e"
  end

  resource "libserf" do
    url "https://repo.msys2.org/msys/x86_64/libserf-1.3.10-2-x86_64.pkg.tar.zst"
    sha256 "e40a917c563591549ed0d6e9dc99a06fd3b4348134f6808e3390d597cd9d482b"
  end

  resource "libsqlite" do
    url "https://repo.msys2.org/msys/x86_64/libsqlite-3.54.0-1-x86_64.pkg.tar.zst"
    sha256 "089e17fe8647da22b790dc2d5629d56cf53b5da5685d6555cb172400f32fe70f"
  end

  resource "libutil-linux" do
    url "https://repo.msys2.org/msys/x86_64/libutil-linux-2.40.2-2-x86_64.pkg.tar.zst"
    sha256 "148ec8e668cb0a30ea0c4e2687e16e0953d6628305598f4c3225bedf2564192d"
  end

  resource "libxcrypt" do
    url "https://repo.msys2.org/msys/x86_64/libxcrypt-4.5.2-1-x86_64.pkg.tar.zst"
    sha256 "e63db0aa2b708359e25f61f506e2d2c92c315febf5f88cc9f386e4ec0bdf4fa5"
  end

  resource "libzstd" do
    url "https://repo.msys2.org/msys/x86_64/libzstd-1.5.7-1-x86_64.pkg.tar.zst"
    sha256 "5e370b2e725a0a7e5ff58d84653c9a0cd5a10b9cfe930f2d949a28ad2a11c1f2"
  end

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  resource "zlib" do
    url "https://repo.msys2.org/msys/x86_64/zlib-1.3.2-1-x86_64.pkg.tar.zst"
    sha256 "a04aa79996c57f0db936be66cf94326d7e67e9cd8dbffe4cf6e97693d0a1d9ef"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/svn.exe", "svn"
    msys2_bin "usr/bin/svnadmin.exe", "svnadmin"
    msys2_bin "usr/bin/svnbench.exe", "svnbench"
    msys2_bin "usr/bin/svndumpfilter.exe", "svndumpfilter"
    msys2_bin "usr/bin/svnfsfs.exe", "svnfsfs"
    msys2_bin "usr/bin/svnlook.exe", "svnlook"
    msys2_bin "usr/bin/svnmucc.exe", "svnmucc"
    msys2_bin "usr/bin/svnrdump.exe", "svnrdump"
    msys2_bin "usr/bin/svnserve.exe", "svnserve"
    msys2_bin "usr/bin/svnsync.exe", "svnsync"
    msys2_bin "usr/bin/svnversion.exe", "svnversion"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/svn --version 2>&1")
  end
end
