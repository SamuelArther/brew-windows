class UBootTools < Formula
  desc "Universal boot loader"
  homepage "https://u-boot-project.org/"
  url "https://repo.msys2.org/msys/x86_64/u-boot-tools-2026.07-1-x86_64.pkg.tar.zst"
  sha256 "f1b2b361479cb1231f497a5a470087e584620f8e92b0125ec4b571bd4f7dde01"
  license all_of: ["GPL-2.0-only", "GPL-2.0-or-later", "BSD-3-Clause"]

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://git.u-boot-project.org/u-boot/u-boot/-/releases/v2026.10/downloads/u-boot-2026.10.tar.bz2"
  end

  resource "bash" do
    url "https://repo.msys2.org/msys/x86_64/bash-5.3.20-2-x86_64.pkg.tar.zst"
    sha256 "0951e809c552dd7dd2835df1b2d590822187f14e717d809e942e4eb677d0adfd"
  end

  resource "dtc" do
    url "https://repo.msys2.org/msys/x86_64/dtc-1.8.1-1-x86_64.pkg.tar.zst"
    sha256 "6406979947745bfd01250654dd3f22c3f5d3717fd8aa682118add8598a2a7c41"
  end

  resource "libopenssl" do
    url "https://repo.msys2.org/msys/x86_64/libopenssl-3.6.5-1-x86_64.pkg.tar.zst"
    sha256 "05f6cc946f0dde3aecc3632007fa1d874e40720226117b750327bc2a01846325"
  end

  resource "libyaml" do
    url "https://repo.msys2.org/msys/x86_64/libyaml-0.2.5-2-x86_64.pkg.tar.zst"
    sha256 "11509c6e3133b3fc63f99cc04e89a1cc5685aa746e96bd5f481e3424fbd4f6c1"
  end

  resource "openssl" do
    url "https://repo.msys2.org/msys/x86_64/openssl-3.6.5-1-x86_64.pkg.tar.zst"
    sha256 "4dc7c1199010b26d6382c59d8b16a9dbc857d314d76d9668f0f333bc5fecdd38"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/dumpimage.exe", "dumpimage"
    msys2_bin "usr/bin/mkenvimage.exe", "mkenvimage"
    msys2_bin "usr/bin/mkimage.exe", "mkimage"
  end

  test do
    system bin/"mkimage", "-V"
    system bin/"dumpimage", "-V"
    system bin/"mkenvimage", "-V"
  end
end
