class Remake < Formula
  desc "GNU Make with improved error handling, tracing, and a debugger"
  homepage "https://bashdb.sourceforge.net/remake/"
  url "https://repo.msys2.org/msys/x86_64/remake-4.3+dbg1.6-1-x86_64.pkg.tar.zst"
  version "4.3-1.6"
  sha256 "ac4b537669f8447d83cd0573b45bde797e3c176ced21aba4d34e00c12cedf3f0"
  license "GPL-3.0-only"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://sourceforge.net/projects/bashdb/files/remake/"
    regex(%r{href=.*?remake/v?(\d+(?:\.\d+)+(?:(?:%2Bdbg)?[._-]\d+(?:\.\d+)+)?)/?["' >]}i)
    strategy :page_match do |page, regex|
      page.scan(regex).map { |match| match&.first&.sub(/%2Bdbg/i, "") }
    end
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "gmp" do
    url "https://repo.msys2.org/msys/x86_64/gmp-6.3.0-2-x86_64.pkg.tar.zst"
    sha256 "f84536138e618d47a1df2da83916cfebbd449f159ffad52f994a71060a41cf34"
  end

  resource "guile" do
    url "https://repo.msys2.org/msys/x86_64/guile-3.0.11-1-x86_64.pkg.tar.zst"
    sha256 "aa773e02f13784e33e8bda363c792c4e9577f42bb37bbc354d5fec669817693a"
  end

  resource "libffi" do
    url "https://repo.msys2.org/msys/x86_64/libffi-3.8.0-1-x86_64.pkg.tar.zst"
    sha256 "e0a21ae5ee3f0ce1ff3f446ea2c95ad462803081014aecfa8fc1cd22bc4df187"
  end

  resource "libgc" do
    url "https://repo.msys2.org/msys/x86_64/libgc-8.2.12-1-x86_64.pkg.tar.zst"
    sha256 "a6bd62a8473c57d3233ab5d4baaf090259fc3a44032d7f1938600c08d2cc2f88"
  end

  resource "libguile" do
    url "https://repo.msys2.org/msys/x86_64/libguile-3.0.11-1-x86_64.pkg.tar.zst"
    sha256 "455c95505ac126fe1d15edb3983c2ffcecbaa0f5b1775dca1d022190b776edc2"
  end

  resource "libiconv" do
    url "https://repo.msys2.org/msys/x86_64/libiconv-1.19-1-x86_64.pkg.tar.zst"
    sha256 "0fa55ea2a6ccf97cf8c58b24b2615815e15e16e6e4e888091c263c2c83c5313d"
  end

  resource "libreadline" do
    url "https://repo.msys2.org/msys/x86_64/libreadline-8.3.006-1-x86_64.pkg.tar.zst"
    sha256 "29e25c50bf6cd7a5e6f2253af76f54fe3c1a915313c7c18e98bedaa665c6adea"
  end

  resource "libunistring" do
    url "https://repo.msys2.org/msys/x86_64/libunistring-1.4.2-1-x86_64.pkg.tar.zst"
    sha256 "bcd04706d95b5e7127d39834565bceed6b400192f5fb51599745aba28d5da018"
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
    msys2_bin "usr/bin/remake.exe", "remake"
  end

  test do
    (testpath/"Makefile").write <<~MAKE
      all:
      	echo "Nothing here, move along"
    MAKE
    system bin/"remake", "-x"
  end
end
