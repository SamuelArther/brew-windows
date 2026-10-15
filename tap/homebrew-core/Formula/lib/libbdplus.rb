class Libbdplus < Formula
  desc "Implements the BD+ System Specifications"
  homepage "https://www.videolan.org/developers/libbdplus.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libbdplus-0.2.0-2-any.pkg.tar.zst"
  sha256 "3c64232ccbc2f687f272df154e51aa8c5d1857a544ba02b1787594f6b7917747"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url :homepage
    regex(/href=.*?libbdplus[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libbdplus/bdplus.h"
    assert_path_exists lib/"libbdplus.dll.a"
    assert_path_exists bin/"libbdplus-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libbdplus.pc").read
  end
end
