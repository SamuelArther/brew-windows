class Telnet < Formula
  desc "User interface to the TELNET protocol"
  homepage "https://opensource.apple.com/"
  version "2.8"
  url "https://repo.msys2.org/msys/x86_64/inetutils-2.8-1-x86_64.pkg.tar.zst"
  sha256 "b0a00adbdc549b54292db23530caf58bb0f150fc39359b99d074d6dee9aed677"
  license "BSD-4-Clause-UC AND APSL-1.0"

  # Homebrew's telnet is Apple's (macOS-only); this is the GNU inetutils telnet client from MSYS2.
  # The version is that of inetutils.

  conflicts_with "inetutils", because: "both install `telnet` binaries"

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
    msys2_bin "usr/bin/telnet.exe", "telnet"
  end

  test do
    assert_match "Connected to", pipe_output("#{bin}/telnet www.google.com 80 2>&1", "GET / HTTP/1.0\r\n\r\n")
  end
end
