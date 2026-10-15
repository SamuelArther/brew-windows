class Folly < Formula
  desc "Collection of reusable C++ library artifacts developed at Facebook"
  homepage "https://github.com/facebook/folly"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-folly-2026.01.26.00-2-any.pkg.tar.zst"
  sha256 "25920b6a289bee44f4ddcb2ca7dc384d2ed883b70fa55cc67ad105b2d2ea385d"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/facebook/folly/archive/refs/tags/v2026.10.12.00.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"folly/Try.h"
  end
end
