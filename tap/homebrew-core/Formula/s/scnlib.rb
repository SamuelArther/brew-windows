class Scnlib < Formula
  desc "Scanf for modern C++"
  homepage "https://www.scnlib.dev/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-scnlib-4.0.1-1-any.pkg.tar.zst"
  sha256 "14c7f9724ab061891e1e9c78f0f1fa79b4cca09d1def8a45fe43b42c8d592f73"
  license "Apache-2.0"
  revision 2

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/eliaskosunen/scnlib/archive/refs/tags/v4.0.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"scn/fwd.h"
    assert_path_exists lib/"libscn.dll.a"
    assert_path_exists bin/"libscn-4.dll"
  end
end
