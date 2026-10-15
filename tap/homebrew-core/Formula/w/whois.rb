class Whois < Formula
  desc "Lookup tool for domain names and other internet resources"
  homepage "https://github.com/rfc1036/whois"
  url "https://repo.msys2.org/msys/x86_64/whois-5.6.6-1-x86_64.pkg.tar.zst"
  sha256 "ba45d068c3948388d586b3e865992b61ec016d85628ba13c40ae9f7a008d8074"
  license "GPL-2.0-or-later"

  # Marco d'Itri's whois via MSYS2.

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

  resource "libunistring" do
    url "https://repo.msys2.org/msys/x86_64/libunistring-1.4.2-1-x86_64.pkg.tar.zst"
    sha256 "bcd04706d95b5e7127d39834565bceed6b400192f5fb51599745aba28d5da018"
  end

  resource "libxcrypt" do
    url "https://repo.msys2.org/msys/x86_64/libxcrypt-4.5.2-1-x86_64.pkg.tar.zst"
    sha256 "e63db0aa2b708359e25f61f506e2d2c92c315febf5f88cc9f386e4ec0bdf4fa5"
  end

  resource "msys2-runtime" do
    url "https://repo.msys2.org/msys/x86_64/msys2-runtime-3.6.11-3-x86_64.pkg.tar.zst"
    sha256 "b6478bd1b4cb4b2e07c45db725fee93f9424c19fa248240dcb7b7f4187cd92db"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/whois.exe", "whois"
  end

  test do
    assert_match "IANA", shell_output("#{bin}/whois -h whois.iana.org com")
  end
end
