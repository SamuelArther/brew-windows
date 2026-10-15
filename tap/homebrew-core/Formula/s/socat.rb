class Socat < Formula
  desc "SOcket CAT: netcat on steroids"
  homepage "http://www.dest-unreach.org/socat/"
  url "https://repo.msys2.org/msys/x86_64/socat-1.8.1.3-1-x86_64.pkg.tar.zst"
  sha256 "ad145b4dfc669c9833601dd277bad550f64f469eed01d9fdea048f6f9a361190"
  license "GPL-2.0-only"

  # socat via MSYS2.

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "libopenssl" do
    url "https://repo.msys2.org/msys/x86_64/libopenssl-3.6.5-1-x86_64.pkg.tar.zst"
    sha256 "05f6cc946f0dde3aecc3632007fa1d874e40720226117b750327bc2a01846325"
  end

  resource "libreadline" do
    url "https://repo.msys2.org/msys/x86_64/libreadline-8.3.006-1-x86_64.pkg.tar.zst"
    sha256 "29e25c50bf6cd7a5e6f2253af76f54fe3c1a915313c7c18e98bedaa665c6adea"
  end

  resource "msys2-runtime" do
    url "https://repo.msys2.org/msys/x86_64/msys2-runtime-3.6.11-3-x86_64.pkg.tar.zst"
    sha256 "b6478bd1b4cb4b2e07c45db725fee93f9424c19fa248240dcb7b7f4187cd92db"
  end

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/socat.exe", "socat"
    msys2_bin "usr/bin/filan.exe", "filan"
    msys2_bin "usr/bin/procan.exe", "procan"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/socat -V")
  end
end
