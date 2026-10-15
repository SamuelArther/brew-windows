class Bootloadhid < Formula
  desc "HID-based USB bootloader for AVR microcontrollers"
  homepage "https://www.obdev.at/products/vusb/bootloadhid.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-bootloadhid-20121208-2-any.pkg.tar.zst"
  version "20121208"
  sha256 "b94fdfe9d743ad96393f01a428abe384c8e23370feed99a34b4a480baad8b006"
  license any_of: ["GPL-2.0-only", "GPL-3.0-only"]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url :homepage
    regex(/href=.*?bootloadHID[._-]v?(\d{4}-\d{1,2}-\d{1,2})\.t/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/bootloadHID.exe", "bootloadHID"
  end

  test do
    touch "test.hex"
    assert_equal "No data in input file, exiting.", shell_output("#{bin}/bootloadHID -r test.hex 2>&1").strip
  end
end
