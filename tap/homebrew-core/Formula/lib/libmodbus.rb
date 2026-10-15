class Libmodbus < Formula
  desc "Portable modbus library"
  homepage "https://libmodbus.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libmodbus-782.ebc4f47-1-any.pkg.tar.zst"
  sha256 "058148897735379f4791bb2f6db088ab5c7fbbc20ed4d76670ccdbfb88034165"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/stephane/libmodbus/releases/download/v3.2.0/libmodbus-3.2.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"modbus/modbus.h"
    assert_path_exists lib/"libmodbus.dll.a"
    assert_path_exists bin/"libmodbus-5.dll"
  end
end
