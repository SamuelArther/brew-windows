class Libexif < Formula
  desc "EXIF parsing library"
  homepage "https://libexif.github.io/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libexif-0.6.26-1-any.pkg.tar.zst"
  sha256 "b1bfcb8f983fcbf7d7078e11fa92aa3a77048e94e317a407bfc6f19c7086f741"
  license all_of: ["LGPL-2.1-or-later", "LGPL-2.0-or-later"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/libexif/libexif/releases/download/v0.6.26/libexif-0.6.26.tar.bz2"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libexif/_stdint.h"
    assert_path_exists lib/"libexif.dll.a"
    assert_path_exists bin/"libexif-12.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libexif.pc").read
  end
end
