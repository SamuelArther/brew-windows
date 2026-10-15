class Nanoarrow < Formula
  desc "Helpers for Arrow C Data & Arrow C Stream interfaces"
  homepage "https://arrow.apache.org/nanoarrow"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-nanoarrow-0.8.0-1-any.pkg.tar.zst"
  sha256 "2c76358b173ca26885c78819563a8820af587c69c7273192c009ff6df4be9541"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/apache/arrow-nanoarrow/releases/download/apache-arrow-nanoarrow-0.9.0/apache-arrow-nanoarrow-0.9.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"nanoarrow/nanoarrow.h"
    assert_path_exists lib/"libnanoarrow.dll.a"
    assert_path_exists bin/"libnanoarrow.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/nanoarrow.pc").read
  end
end
