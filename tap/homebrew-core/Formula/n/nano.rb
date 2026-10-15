class Nano < Formula
  desc "Free (GNU) replacement for the Pico text editor"
  homepage "https://www.nano-editor.org/"
  url "https://repo.msys2.org/msys/x86_64/nano-9.2-1-x86_64.pkg.tar.zst"
  sha256 "0fd23077af18ab656e4a082ce0455ab16217c852f3968343e903843c204a12b2"
  license "GPL-3.0-or-later"

  # GNU nano via MSYS2; run it in a real console (Windows Terminal, conhost or mintty).

  resource "bash" do
    url "https://repo.msys2.org/msys/x86_64/bash-5.3.20-2-x86_64.pkg.tar.zst"
    sha256 "0951e809c552dd7dd2835df1b2d590822187f14e717d809e942e4eb677d0adfd"
  end

  resource "file" do
    url "https://repo.msys2.org/msys/x86_64/file-5.48-1-x86_64.pkg.tar.zst"
    sha256 "f55026638fe4ed4b31ee3cf316843ac190d9332816bff8dcb0a99fd8c6350608"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "libbz2" do
    url "https://repo.msys2.org/msys/x86_64/libbz2-1.0.8-4-x86_64.pkg.tar.zst"
    sha256 "89fb0e1478b22b80effda55ab4ae7549388d01245d3841a096d8a2d63236c2a1"
  end

  resource "libiconv" do
    url "https://repo.msys2.org/msys/x86_64/libiconv-1.19-1-x86_64.pkg.tar.zst"
    sha256 "0fa55ea2a6ccf97cf8c58b24b2615815e15e16e6e4e888091c263c2c83c5313d"
  end

  resource "libintl" do
    url "https://repo.msys2.org/msys/x86_64/libintl-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "336d66b9d95cf9c1804958f8e260762a3e83bf158ed5981f783bc772a31073cf"
  end

  resource "liblzma" do
    url "https://repo.msys2.org/msys/x86_64/liblzma-5.8.4-1-x86_64.pkg.tar.zst"
    sha256 "4b7d61656e72a5651a797680b66186338dce4f8fbc62a637ab3b59922dd01074"
  end

  resource "libzstd" do
    url "https://repo.msys2.org/msys/x86_64/libzstd-1.5.7-1-x86_64.pkg.tar.zst"
    sha256 "5e370b2e725a0a7e5ff58d84653c9a0cd5a10b9cfe930f2d949a28ad2a11c1f2"
  end

  resource "msys2-runtime" do
    url "https://repo.msys2.org/msys/x86_64/msys2-runtime-3.6.11-3-x86_64.pkg.tar.zst"
    sha256 "b6478bd1b4cb4b2e07c45db725fee93f9424c19fa248240dcb7b7f4187cd92db"
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
    msys2_bin "usr/bin/nano.exe", "nano"
    msys2_bin "usr/bin/rnano.exe", "rnano"
  end

  test do
    system bin/"nano", "--version"
  end
end
