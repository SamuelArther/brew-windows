class Hidapi < Formula
  desc "Library for communicating with USB and Bluetooth HID devices"
  homepage "https://libusb.info/hidapi/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-hidapi-0.15.0-2-any.pkg.tar.zst"
  sha256 "a514e6bfdc5b9b76798d6892905ae68d462a63e90ecb6b23c73510dbf10f028d"
  license any_of: ["GPL-3.0-only", "BSD-3-Clause", "HIDAPI"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/libusb/hidapi/archive/refs/tags/hidapi-0.15.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"hidapi/hidapi.h"
    assert_path_exists lib/"libhidapi.dll.a"
    assert_path_exists bin/"libhidapi-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/hidapi.pc").read
  end
end
