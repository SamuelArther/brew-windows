class Libfido2 < Formula
  desc "Provides library functionality for FIDO U2F & FIDO 2.0, including USB"
  homepage "https://developers.yubico.com/libfido2/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libfido2-1.17.0-3-any.pkg.tar.zst"
  sha256 "f90a6189334a9653585eec66aa91d28b73fbd4d84095a2a241a78340540eff1d"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/Yubico/libfido2/archive/refs/tags/1.17.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"fido.h"
    assert_path_exists lib/"libfido2.dll.a"
    assert_path_exists bin/"libfido2-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libfido2.pc").read
  end
end
