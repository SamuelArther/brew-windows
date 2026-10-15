class Freealut < Formula
  desc "Implementation of OpenAL's ALUT standard"
  homepage "https://github.com/vancegroup/freealut"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-freealut-1.1.0-3-any.pkg.tar.zst"
  sha256 "2bd27671486d7afc6356789ddc78fea655760fbaa1ee3ba77ac9c86f6a1e5294"
  license "LGPL-2.0-only"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://deb.debian.org/debian/pool/main/f/freealut/"
    regex(/href=.*?freealut[._-]v?(\d+(?:\.\d+)+)\.orig\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"AL/alut.h"
    assert_path_exists lib/"libalut.dll.a"
    assert_path_exists bin/"libalut-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/freealut.pc").read
  end
end
