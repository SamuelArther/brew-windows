class LibusbCompat < Formula
  desc "Library for USB device access"
  homepage "https://libusb.info/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libusb-compat-r76.b5db9d0-2-any.pkg.tar.zst"
  version "r76.b5db9d0"
  sha256 "fe53b39ff756f4f86f602d2dea95c6066fe2b54864ca8f2f1b5c7cef7b3c372a"
  license all_of: [
    "LGPL-2.1-or-later",
    any_of: ["LGPL-2.1-or-later", "BSD-3-Clause"], # libusb/usb.h
  ]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/libusb/libusb-compat-0.1/libusb-compat-0.1.9/libusb-compat-0.1.9.tar.bz2"
    regex(%r{url=.*?/libusb-compat[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libusb-compat/usb.h"
    assert_path_exists lib/"libusb.dll.a"
    assert_path_exists bin/"libusb-0-1-4.dll"
  end
end
