class Oniguruma < Formula
  desc "Regular expressions library"
  homepage "https://github.com/kkos/oniguruma/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-oniguruma-6.9.10-1-any.pkg.tar.zst"
  sha256 "e1132938ebc40f8d820fae202c3de1493be3a606bd454c9e1c619ed730dd5697"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    skip "No longer developed or maintained"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"oniggnu.h"
    assert_path_exists lib/"libonig.dll.a"
    assert_path_exists bin/"libonig-5.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/oniguruma.pc").read
  end
end
