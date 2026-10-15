class Libimobiledevice < Formula
  desc "Library to communicate with iOS devices natively"
  homepage "https://www.libimobiledevice.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libimobiledevice-1.3.0-17-any.pkg.tar.zst"
  sha256 "2f8ede1528372fa0ec7d3ec9f248b22c43e3bb2f573025c9086781413d12a1d1"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/libimobiledevice/libimobiledevice/releases/download/1.4.0/libimobiledevice-1.4.0.tar.bz2"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libimobiledevice-glue" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libimobiledevice-glue-1.3.2-1-any.pkg.tar.zst"
    sha256 "d089c718ab846c5bd1f41fb2788a976fdbda13c9f09128a670eca578da0bc34f"
  end

  resource "mingw-w64-ucrt-x86_64-libplist" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libplist-2.7.0-4-any.pkg.tar.zst"
    sha256 "665607c34dbf923ebb820d78b376db59de4d2d3874a45d70f24f51eb1d64f127"
  end

  resource "mingw-w64-ucrt-x86_64-libstdc++" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libstdc++-16.2.0-6-any.pkg.tar.zst"
    sha256 "33c5e58a85a538cf843a8672991cce591f0231d66da3b66a57629712d64be17f"
  end

  resource "mingw-w64-ucrt-x86_64-libusbmuxd" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libusbmuxd-2.1.1-1-any.pkg.tar.zst"
    sha256 "27c86acc00eda349947fa0fde05cfca805ab1aee6da4cc4e4a6486aedf054f1c"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-openssl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openssl-3.6.5-1-any.pkg.tar.zst"
    sha256 "773e021ebad83f14b5c103db467411e8ff064663e1c7e7f730e2be72f4117064"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/idevice_id.exe", "idevice_id"
    msys2_bin "ucrt64/bin/idevicebackup.exe", "idevicebackup"
    msys2_bin "ucrt64/bin/idevicebackup2.exe", "idevicebackup2"
    msys2_bin "ucrt64/bin/idevicecrashreport.exe", "idevicecrashreport"
    msys2_bin "ucrt64/bin/idevicedate.exe", "idevicedate"
    msys2_bin "ucrt64/bin/idevicedebug.exe", "idevicedebug"
    msys2_bin "ucrt64/bin/idevicedebugserverproxy.exe", "idevicedebugserverproxy"
    msys2_bin "ucrt64/bin/idevicediagnostics.exe", "idevicediagnostics"
    msys2_bin "ucrt64/bin/ideviceenterrecovery.exe", "ideviceenterrecovery"
    msys2_bin "ucrt64/bin/ideviceimagemounter.exe", "ideviceimagemounter"
    msys2_bin "ucrt64/bin/ideviceinfo.exe", "ideviceinfo"
    msys2_bin "ucrt64/bin/idevicename.exe", "idevicename"
    msys2_bin "ucrt64/bin/idevicenotificationproxy.exe", "idevicenotificationproxy"
    msys2_bin "ucrt64/bin/idevicepair.exe", "idevicepair"
    msys2_bin "ucrt64/bin/ideviceprovision.exe", "ideviceprovision"
    msys2_bin "ucrt64/bin/idevicescreenshot.exe", "idevicescreenshot"
    msys2_bin "ucrt64/bin/idevicesetlocation.exe", "idevicesetlocation"
    msys2_bin "ucrt64/bin/idevicesyslog.exe", "idevicesyslog"
  end

  test do
    system bin/"idevicedate", "--help"
  end
end
