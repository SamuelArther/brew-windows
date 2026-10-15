class DfuProgrammer < Formula
  desc "Device firmware update based USB programmer for Atmel chips"
  homepage "https://github.com/dfu-programmer/dfu-programmer"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-dfu-programmer-1.1.0-1-any.pkg.tar.zst"
  sha256 "025e542e11e2a503564689657b86437963190c12ef4dffd85f54140e5bcf661d"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/dfu-programmer/dfu-programmer/releases/download/v1.1.0/dfu-programmer-1.1.0.tar.gz"
  end

  resource "mingw-w64-ucrt-x86_64-libusb" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libusb-1.0.30-1-any.pkg.tar.zst"
    sha256 "20106e6ff31b4581bb111b28a68b6b79a50dc3e5fc3033944e1c4b3ff8cd812e"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/dfu-programmer.exe", "dfu-programmer"
  end

  test do
    assert_match "8051 based controllers", shell_output("#{bin}/dfu-programmer --targets")
    assert_match version.to_s, shell_output("#{bin}/dfu-programmer --version 2>&1")
  end
end
