class Gflags < Formula
  desc "Library for processing command-line flags"
  homepage "https://gflags.github.io/gflags/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gflags-2.3.0-1-any.pkg.tar.zst"
  sha256 "ad16b6b48d3f10edd880297dfcdaa7efbf17c3cfffd6e72d1e7a9c69d6259b5b"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/gflags/gflags/archive/refs/tags/v2.3.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"gflags/gflags.h"
    assert_path_exists lib/"libgflags.dll.a"
    assert_path_exists bin/"libgflags.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/gflags.pc").read
  end
end
