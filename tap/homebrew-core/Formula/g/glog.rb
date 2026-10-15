class Glog < Formula
  desc "Application-level logging library"
  homepage "https://google.github.io/glog/stable/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-glog-0.7.1-10-any.pkg.tar.zst"
  sha256 "eafd5d29a59890a5f102ece7749b2631c310910fc216752be6b20286754cd5f7"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/google/glog/archive/refs/tags/v0.7.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"glog/flags.h"
    assert_path_exists lib/"libglog.dll.a"
    assert_path_exists bin/"libglog-2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libglog.pc").read
  end
end
