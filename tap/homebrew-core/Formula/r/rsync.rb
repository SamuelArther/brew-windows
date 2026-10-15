class Rsync < Formula
  desc "Utility that provides fast incremental file transfer"
  homepage "https://rsync.samba.org/"
  url "https://repo.msys2.org/msys/x86_64/rsync-3.5.1-2-x86_64.pkg.tar.zst"
  sha256 "cba52e41d16f9873324fcf09853619964da87bc1ce116781a5449c968d4ffecc"
  license "GPL-3.0-or-later"

  # rsync via MSYS2. Remote transfers need an ssh on PATH; local copies work as is.

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "libiconv" do
    url "https://repo.msys2.org/msys/x86_64/libiconv-1.19-1-x86_64.pkg.tar.zst"
    sha256 "0fa55ea2a6ccf97cf8c58b24b2615815e15e16e6e4e888091c263c2c83c5313d"
  end

  resource "libidn2" do
    url "https://repo.msys2.org/msys/x86_64/libidn2-2.3.8-1-x86_64.pkg.tar.zst"
    sha256 "55e0de96b893e9f67f14a4283d411fc2a45d7b59333bc16e2a9b16e5bfc9282f"
  end

  resource "libintl" do
    url "https://repo.msys2.org/msys/x86_64/libintl-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "336d66b9d95cf9c1804958f8e260762a3e83bf158ed5981f783bc772a31073cf"
  end

  resource "liblz4" do
    url "https://repo.msys2.org/msys/x86_64/liblz4-1.10.0-1-x86_64.pkg.tar.zst"
    sha256 "1cbbe51ee91bf32691aabb15bdb7c8548e8b5ef8195122ef31f10edcca6e952f"
  end

  resource "libopenssl" do
    url "https://repo.msys2.org/msys/x86_64/libopenssl-3.6.5-1-x86_64.pkg.tar.zst"
    sha256 "05f6cc946f0dde3aecc3632007fa1d874e40720226117b750327bc2a01846325"
  end

  resource "libunistring" do
    url "https://repo.msys2.org/msys/x86_64/libunistring-1.4.2-1-x86_64.pkg.tar.zst"
    sha256 "bcd04706d95b5e7127d39834565bceed6b400192f5fb51599745aba28d5da018"
  end

  resource "libxxhash" do
    url "https://repo.msys2.org/msys/x86_64/libxxhash-0.8.4-1-x86_64.pkg.tar.zst"
    sha256 "ba784ad117626401cd5d3ec0cf70399f9de72a63755f96c28fa2032303f9c330"
  end

  resource "libzstd" do
    url "https://repo.msys2.org/msys/x86_64/libzstd-1.5.7-1-x86_64.pkg.tar.zst"
    sha256 "5e370b2e725a0a7e5ff58d84653c9a0cd5a10b9cfe930f2d949a28ad2a11c1f2"
  end

  resource "msys2-runtime" do
    url "https://repo.msys2.org/msys/x86_64/msys2-runtime-3.6.11-3-x86_64.pkg.tar.zst"
    sha256 "b6478bd1b4cb4b2e07c45db725fee93f9424c19fa248240dcb7b7f4187cd92db"
  end

  resource "popt" do
    url "https://repo.msys2.org/msys/x86_64/popt-1.19-1-x86_64.pkg.tar.zst"
    sha256 "b402bd1d03815c5e28b7ab415d8e20f487eff2460ae0271a99674eeb0d77194f"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/rsync.exe", "rsync"
  end

  test do
    mkdir "a"
    (testpath/"a/foo").write "bar"
    system bin/"rsync", "-a", "a/", "b"
    assert_equal "bar", (testpath/"b/foo").read
  end
end
