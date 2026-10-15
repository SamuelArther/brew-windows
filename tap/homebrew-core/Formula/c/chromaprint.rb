class Chromaprint < Formula
  desc "Core component of the AcoustID project (Audio fingerprinting)"
  homepage "https://acoustid.org/chromaprint"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-chromaprint-1.6.1-1-any.pkg.tar.zst"
  sha256 "24dceb792eaca148cbfaf9f14c3a3ef58a8d7b8205f3e0c5d31944efece37fd3"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/acoustid/chromaprint/releases/download/v1.6.2/chromaprint-1.6.2.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"chromaprint.h"
    assert_path_exists lib/"libchromaprint.dll.a"
    assert_path_exists bin/"libchromaprint.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libchromaprint.pc").read
  end
end
