class Tgui < Formula
  desc "GUI library for use with sfml"
  homepage "https://tgui.eu"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tgui-1.13.0-1-any.pkg.tar.zst"
  sha256 "7500c71b66e6cea1de09c857153525ccb5a30aadc1fd3dd404598ae657ca5e8d"
  license "Zlib"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/texus/TGUI/archive/refs/tags/v1.13.0.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"TGUI/Any.hpp"
    assert_path_exists bin/"tgui.dll"
  end
end
