class M4rie < Formula
  desc "Library for fast arithmetic with dense matrices over GF(2^e), 2<=e<=16"
  homepage "https://bitbucket.org/malb/m4rie"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-m4rie-20250128-2-any.pkg.tar.zst"
  version "20250128"
  sha256 "b99594ed0f29e0dffb64ce2e7180db36875976be22ec4eaeda43aa4791f0f604"
  license "GPL-2.0-only"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/malb/m4rie/releases/download/20250128/m4rie-20250128.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"m4rie/blm.h"
    assert_path_exists lib/"libm4rie.dll.a"
    assert_path_exists bin/"libm4rie-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/m4rie.pc").read
  end
end
