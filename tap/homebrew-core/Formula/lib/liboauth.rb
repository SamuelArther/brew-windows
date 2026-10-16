class Liboauth < Formula
  desc "C library for the OAuth Core RFC 5849 standard"
  homepage "https://sourceforge.net/projects/liboauth/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-liboauth-1.0.3-7-any.pkg.tar.zst"
  sha256 "f1182f95b9cc2b108d0e57f275cfb23c796154e930c68465b80f70f507f76257"
  license "MIT"
  revision 4

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/liboauth/liboauth-1.0.3.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"oauth.h"
    assert_path_exists lib/"liboauth.dll.a"
    assert_path_exists bin/"liboauth-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/oauth.pc").read
  end
end
