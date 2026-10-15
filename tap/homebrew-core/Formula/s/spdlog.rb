class Spdlog < Formula
  desc "Super fast C++ logging library"
  homepage "https://github.com/gabime/spdlog"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-spdlog-1.17.0-3-any.pkg.tar.zst"
  sha256 "5886840f5d914195412ef73e4249a2f1e3284dd9c572829e2083083e689025e5"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/gabime/spdlog/archive/refs/tags/v1.17.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"spdlog/fwd.h"
    assert_path_exists lib/"libspdlog.dll.a"
    assert_path_exists bin/"libspdlog-1.17.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/spdlog.pc").read
  end
end
