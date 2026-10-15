class Openfa < Formula
  desc "Set of algorithms that implement standard models used in fundamental astronomy"
  homepage "https://gitlab.obspm.fr/imcce_openfa/openfa"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openfa-20231011.0.2-2-any.pkg.tar.zst"
  sha256 "b11ce3f45c8c734165219d650df8dc6b809c9013d02e88c26471a38f2842f2cb"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://gitlab.obspm.fr/imcce_openfa/openfa/-/archive/20231011.0.3/openfa-20231011.0.3.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"openfa.h"
    assert_path_exists lib/"libopenfa.dll.a"
    assert_path_exists bin/"libopenfa.dll"
  end
end
