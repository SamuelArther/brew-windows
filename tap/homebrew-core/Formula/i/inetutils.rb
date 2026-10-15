class Inetutils < Formula
  desc "GNU utilities for networking"
  homepage "https://www.gnu.org/software/inetutils/"
  url "https://repo.msys2.org/msys/x86_64/inetutils-2.8-1-x86_64.pkg.tar.zst"
  sha256 "b0a00adbdc549b54292db23530caf58bb0f150fc39359b99d074d6dee9aed677"
  license "GPL-3.0-or-later"

  # GNU inetutils clients via MSYS2 (MSYS2 builds dnsdomainname, ftp, rcp, rlogin, rsh, talk and telnet).

  conflicts_with "telnet", because: "both install `telnet` binaries"
  conflicts_with "gping", because: "both install `gping` binaries"

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
    msys2_bin "usr/bin/dnsdomainname.exe", "dnsdomainname"
    msys2_bin "usr/bin/ftp.exe", "ftp"
    msys2_bin "usr/bin/rcp.exe", "rcp"
    msys2_bin "usr/bin/rlogin.exe", "rlogin"
    msys2_bin "usr/bin/rsh.exe", "rsh"
    msys2_bin "usr/bin/talk.exe", "talk"
    msys2_bin "usr/bin/telnet.exe", "telnet"
  end

  test do
    assert_match "GNU inetutils", shell_output("#{bin}/ftp --version")
  end
end
