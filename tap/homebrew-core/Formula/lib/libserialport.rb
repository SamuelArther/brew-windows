class Libserialport < Formula
  desc "Cross-platform serial port C library"
  homepage "https://sigrok.org/wiki/Libserialport"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libserialport-0.1.2-1-any.pkg.tar.zst"
  sha256 "20413ea0b338cdab5dad13ca46a23590bd6a3762cdef0bee007f3abcc48befa4"
  license "LGPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://sigrok.org/wiki/Downloads"
    regex(/href=.*?libserialport[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libserialport.h"
    assert_path_exists lib/"libserialport.dll.a"
    assert_path_exists bin/"libserialport-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libserialport.pc").read
  end
end
