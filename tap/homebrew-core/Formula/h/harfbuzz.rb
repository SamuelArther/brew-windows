class Harfbuzz < Formula
  desc "OpenType text shaping engine"
  homepage "https://github.com/harfbuzz/harfbuzz"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-harfbuzz-14.6.0-1-any.pkg.tar.zst"
  sha256 "8781d486c864bf91f482768cc5bd7267e49331636583d9f855af3974e6378e69"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/harfbuzz/harfbuzz/releases/download/14.6.0/harfbuzz-14.6.0.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"harfbuzz/hb.h"
    assert_path_exists lib/"libharfbuzz-gobject.dll.a"
    assert_path_exists bin/"libharfbuzz-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/harfbuzz-gobject.pc").read
  end
end
