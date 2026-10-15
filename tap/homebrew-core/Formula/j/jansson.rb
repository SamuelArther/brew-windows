class Jansson < Formula
  desc "C library for encoding, decoding, and manipulating JSON"
  homepage "https://digip.org/jansson/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-jansson-2.15.1-1-any.pkg.tar.zst"
  sha256 "ea369738b89a8e74e5e3a109e7db39c9a973fd5a8c3d16afda359b44ec4e123e"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/akheron/jansson/releases/download/v2.15.1/jansson-2.15.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"jansson.h"
    assert_path_exists lib/"libjansson.dll.a"
    assert_path_exists bin/"libjansson-4.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/jansson.pc").read
  end
end
