class Oatpp < Formula
  desc "Light and powerful C++ web framework"
  homepage "https://oatpp.io/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-oatpp-1.3.1-3-any.pkg.tar.zst"
  sha256 "37589a7074a34c9af6383652128c4ee2bec7f38acd4a25398c64ae262b06f8b9"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/oatpp/oatpp/archive/refs/tags/1.3.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"oatpp-1.3.0/oatpp/oatpp-test/Checker.hpp"
  end
end
