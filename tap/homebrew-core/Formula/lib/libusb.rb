class Libusb < Formula
  desc "Library for USB device access"
  homepage "https://libusb.info/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libusb-1.0.30-1-any.pkg.tar.zst"
  sha256 "20106e6ff31b4581bb111b28a68b6b79a50dc3e5fc3033944e1c4b3ff8cd812e"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/libusb/libusb/releases/download/v1.0.30/libusb-1.0.30.tar.bz2"
    strategy :github_latest
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libusb-1.0/libusb.h"
    assert_path_exists lib/"libusb-1.0.dll.a"
    assert_path_exists bin/"libusb-1.0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libusb-1.0.pc").read
  end
end
