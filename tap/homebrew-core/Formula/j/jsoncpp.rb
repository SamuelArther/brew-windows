class Jsoncpp < Formula
  desc "Library for interacting with JSON"
  homepage "https://github.com/open-source-parsers/jsoncpp"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-jsoncpp-1.9.8-1-any.pkg.tar.zst"
  sha256 "c8bf6af2e6d7972f748f738ba8e510777437dcfd50846c378bc6e0990ce7ce2d"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/open-source-parsers/jsoncpp/archive/refs/tags/1.9.8.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"json/json.h"
    assert_path_exists lib/"libjsoncpp.dll.a"
    assert_path_exists bin/"libjsoncpp-27.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/jsoncpp.pc").read
  end
end
