class Stlink < Formula
  desc "STM32 discovery line Linux programmer"
  homepage "https://github.com/stlink-org/stlink"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-stlink-1.8.0-1-any.pkg.tar.zst"
  sha256 "90a7333f0b5b107594b8e3f7b40da5cdd96898337e3068d38fab3d6c297a0b9a"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/stlink-org/stlink/archive/refs/tags/v1.9.0.tar.gz"
  end

  resource "mingw-w64-ucrt-x86_64-libusb" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libusb-1.0.30-1-any.pkg.tar.zst"
    sha256 "20106e6ff31b4581bb111b28a68b6b79a50dc3e5fc3033944e1c4b3ff8cd812e"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/st-flash.exe", "st-flash"
    msys2_bin "ucrt64/bin/st-info.exe", "st-info"
    msys2_bin "ucrt64/bin/st-trace.exe", "st-trace"
    msys2_bin "ucrt64/bin/st-util.exe", "st-util"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/st-flash --version 2>&1")
  end
end
