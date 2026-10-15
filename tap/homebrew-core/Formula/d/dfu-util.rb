class DfuUtil < Formula
  desc "USB programmer"
  homepage "https://dfu-util.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-dfu-util-0.11-2-any.pkg.tar.zst"
  sha256 "62e250375374fd272ef49bc5704e0d63f490e4b6d54b3f16f121fb190b53721d"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://downloads.sourceforge.net/project/dfu-util/dfu-util-0.11.tar.gz"
  end

  resource "mingw-w64-ucrt-x86_64-libusb" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libusb-1.0.30-1-any.pkg.tar.zst"
    sha256 "20106e6ff31b4581bb111b28a68b6b79a50dc3e5fc3033944e1c4b3ff8cd812e"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/dfu-prefix.exe", "dfu-prefix"
    msys2_bin "ucrt64/bin/dfu-suffix.exe", "dfu-suffix"
    msys2_bin "ucrt64/bin/dfu-util.exe", "dfu-util"
  end

  test do
    system bin/"dfu-util", "-V"
    system bin/"dfu-prefix", "-V"
    system bin/"dfu-suffix", "-V"
  end
end
