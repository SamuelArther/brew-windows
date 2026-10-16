class Unzip < Formula
  desc "Extraction utility for .zip compressed archives"
  homepage "https://infozip.sourceforge.net/UnZip.html"
  url "https://repo.msys2.org/msys/x86_64/unzip-6.0-3-x86_64.pkg.tar.zst"
  sha256 "c98ebac31ea92a63cf61c6190ed3e8284ccc0c29c43973f1b2c0de2874e5acfe"
  license "Info-ZIP"
  revision 8

  # Info-ZIP UnZip via MSYS2.

  resource "bash" do
    url "https://repo.msys2.org/msys/x86_64/bash-5.3.20-2-x86_64.pkg.tar.zst"
    sha256 "0951e809c552dd7dd2835df1b2d590822187f14e717d809e942e4eb677d0adfd"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "libbz2" do
    url "https://repo.msys2.org/msys/x86_64/libbz2-1.0.8-4-x86_64.pkg.tar.zst"
    sha256 "89fb0e1478b22b80effda55ab4ae7549388d01245d3841a096d8a2d63236c2a1"
  end

  resource "msys2-runtime" do
    url "https://repo.msys2.org/msys/x86_64/msys2-runtime-3.6.11-3-x86_64.pkg.tar.zst"
    sha256 "b6478bd1b4cb4b2e07c45db725fee93f9424c19fa248240dcb7b7f4187cd92db"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/unzip.exe", "unzip"
    msys2_bin "usr/bin/funzip.exe", "funzip"
    msys2_bin "usr/bin/unzipsfx.exe", "unzipsfx"
    msys2_bin "usr/bin/zipinfo.exe", "zipinfo"
  end

  test do
    system bin/"unzip", "-v"
  end
end
