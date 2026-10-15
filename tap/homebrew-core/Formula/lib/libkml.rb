class Libkml < Formula
  desc "Library to parse, generate and operate on KML"
  homepage "https://github.com/libkml/libkml"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libkml-1.3.0-12-any.pkg.tar.zst"
  sha256 "662675af055f8990175cad54ebca009f04c4162f738dd172d988181e23cfe81a"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/libkml/libkml/archive/refs/tags/1.3.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"kml/dom.h"
    assert_path_exists lib/"libkmlbase.dll.a"
    assert_path_exists bin/"libkmlbase.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libkml.pc").read
  end
end
