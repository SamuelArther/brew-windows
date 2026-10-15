class Glm < Formula
  desc "C++ mathematics library for graphics software"
  homepage "https://glm.g-truc.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-glm-1.0.3-1-any.pkg.tar.zst"
  sha256 "8f7fbf07967af63ddaa1ea93899e041b2322a3ff1c03c3b08c71f59eb0d7a3c3"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/g-truc/glm/archive/refs/tags/1.0.3.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"glm/ext.hpp"
  end
end
