class Patchutils < Formula
  desc "Small collection of programs that operate on patch files"
  homepage "https://cyberelk.net/tim/software/patchutils/"
  url "https://repo.msys2.org/msys/x86_64/patchutils-0.4.3-1-x86_64.pkg.tar.zst"
  sha256 "81dd1fdb4e12c1aa2ed5c4ed7a8f31da3dfc9d73d29ab4ae905813bd63da555b"
  license all_of: ["GPL-2.0-or-later", "LGPL-2.1-or-later"]

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://github.com/twaugh/patchutils/releases/download/0.4.5/patchutils-0.4.5.tar.xz"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "libbz2" do
    url "https://repo.msys2.org/msys/x86_64/libbz2-1.0.8-4-x86_64.pkg.tar.zst"
    sha256 "89fb0e1478b22b80effda55ab4ae7549388d01245d3841a096d8a2d63236c2a1"
  end

  resource "libpcre2_16" do
    url "https://repo.msys2.org/msys/x86_64/libpcre2_16-10.49-1-x86_64.pkg.tar.zst"
    sha256 "3e778e2ccb0800566031a473bc0d81fd9c731b458bf77fc2758d69501454312d"
  end

  resource "libpcre2_32" do
    url "https://repo.msys2.org/msys/x86_64/libpcre2_32-10.49-1-x86_64.pkg.tar.zst"
    sha256 "0de7784c615309bf05565c5963741cee43ba440187871bb29c88ca4e26000fb4"
  end

  resource "libpcre2_8" do
    url "https://repo.msys2.org/msys/x86_64/libpcre2_8-10.49-1-x86_64.pkg.tar.zst"
    sha256 "2de42bf4656e3952a72809663e85899be76c7020e016687055ac0acd05145a3b"
  end

  resource "libpcre2posix" do
    url "https://repo.msys2.org/msys/x86_64/libpcre2posix-10.49-1-x86_64.pkg.tar.zst"
    sha256 "5d047f5e4d134ae4c39a5e63056680a5abbd97c07f35c3e2afd56755e4bb7042"
  end

  resource "libreadline" do
    url "https://repo.msys2.org/msys/x86_64/libreadline-8.3.006-1-x86_64.pkg.tar.zst"
    sha256 "29e25c50bf6cd7a5e6f2253af76f54fe3c1a915313c7c18e98bedaa665c6adea"
  end

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  resource "pcre2" do
    url "https://repo.msys2.org/msys/x86_64/pcre2-10.49-1-x86_64.pkg.tar.zst"
    sha256 "e3611bbe2eb9ea4b79e316618dd819bfd148aa06a04a4b64aec0348e3a52eac8"
  end

  resource "zlib" do
    url "https://repo.msys2.org/msys/x86_64/zlib-1.3.2-1-x86_64.pkg.tar.zst"
    sha256 "a04aa79996c57f0db936be66cf94326d7e67e9cd8dbffe4cf6e97693d0a1d9ef"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/combinediff.exe", "combinediff"
    msys2_bin "usr/bin/filterdiff.exe", "filterdiff"
    msys2_bin "usr/bin/flipdiff.exe", "flipdiff"
    msys2_bin "usr/bin/grepdiff.exe", "grepdiff"
    msys2_bin "usr/bin/interdiff.exe", "interdiff"
    msys2_bin "usr/bin/lsdiff.exe", "lsdiff"
    msys2_bin "usr/bin/patchview.exe", "patchview"
    msys2_bin "usr/bin/rediff.exe", "rediff"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/combinediff --version 2>&1")
  end
end
