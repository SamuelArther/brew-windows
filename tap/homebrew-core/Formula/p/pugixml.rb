class Pugixml < Formula
  desc "Light-weight C++ XML processing library"
  homepage "https://pugixml.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-pugixml-1.16-1-any.pkg.tar.zst"
  sha256 "3b9460d0f25c5858ddcedb8aafdd28b9fddcf3499ca677b3b5329cd00d311660"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/zeux/pugixml/releases/download/v1.16/pugixml-1.16.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"pugixml.hpp"
    assert_path_exists lib/"libpugixml.dll.a"
    assert_path_exists bin/"libpugixml.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/pugixml.pc").read
  end
end
