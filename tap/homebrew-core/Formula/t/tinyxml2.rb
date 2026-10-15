class Tinyxml2 < Formula
  desc "Improved tinyxml (in memory efficiency and size)"
  homepage "https://leethomason.github.io/tinyxml2/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tinyxml2-11.0.0-2-any.pkg.tar.zst"
  sha256 "cff73fca6458458fc4298024dfcc0aa9cb86c56b44050fec59762d3f622c4c4f"
  license "Zlib"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/leethomason/tinyxml2/archive/refs/tags/11.0.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"tinyxml2.h"
    assert_path_exists lib/"libtinyxml2.dll.a"
    assert_path_exists bin/"libtinyxml2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/tinyxml2.pc").read
  end
end
